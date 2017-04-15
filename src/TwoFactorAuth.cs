// Copyright (C) 2017 Dmitry Yakimenko (detunized@gmail.com).
// Licensed under the terms of the MIT license. See LICENCE for details.

using System;
using System.Linq;

namespace TrueKey
{
    public class TwoFactorAuth
    {
        public enum Step
        {
            Done = 10,
            WaitForOob = 12,
            ChooseOob = 13,
            WaitForEmail = 14,
        }

        public class Settings
        {
            public readonly Step InitialStep;
            public readonly string TransactionId;
            public readonly string Email;
            public readonly OobDevice[] Devices;
            public readonly string OAuthToken;

            public Settings(Step initialStep,
                            string transactionId,
                            string email,
                            OobDevice[] devices,
                            string oAuthToken)
            {
                InitialStep = initialStep;
                TransactionId = transactionId;
                Email = email;
                Devices = devices;
                OAuthToken = oAuthToken;
            }
        }

        public class OobDevice
        {
            public readonly string Name;
            public readonly string Id;

            public OobDevice(string name, string id)
            {
                Name = name;
                Id = id;
            }
        }

        public abstract class Gui
        {
            public enum Answer
            {
                Check,
                Resend,
                Email,
                Device0,
            }

            public abstract Answer AskToWaitForEmail(string email, Answer[] validAnswers);
            public abstract Answer AskToWaitForOob(string name, string email, Answer[] validAnswers);
            public abstract Answer AskToChooseOob(string[] names, string email, Answer[] validAnswers);
        }

        public static string Start(Remote.ClientInfo clientInfo, Settings settings, Gui gui)
        {
            return Start(clientInfo, settings, gui, new HttpClient());
        }

        public static string Start(Remote.ClientInfo clientInfo, Settings settings, Gui gui, IHttpClient http)
        {
            return new TwoFactorAuth(clientInfo, settings, gui, http).Run(settings.InitialStep);
        }

        //
        // private
        //

        private abstract class State
        {
            public virtual bool IsDone
            {
                get { return false; }
            }

            public virtual bool IsSuccess
            {
                get { return false; }
            }

            public virtual string Result
            {
                get { throw new InvalidOperationException("Unreachable code"); }
            }

            public virtual State Advance(TwoFactorAuth owner)
            {
                throw new InvalidOperationException("Unreachable code");
            }

            // TODO: Shared code for most states. It's not really good that it's in the base class.
            protected State Check(TwoFactorAuth owner)
            {
                var result = Remote.AuthCheck(owner._clientInfo, owner._http);
                if (result == null)
                    return new Failure("Failed");
                return new Done(result);
            }
        }

        private class Done: State
        {
            public Done(string oAuthToken)
            {
                _oAuthToken = oAuthToken;
            }

            public override bool IsDone
            {
                get { return true; }
            }

            public override bool IsSuccess
            {
                get { return true; }
            }

            public override string Result
            {
                get { return _oAuthToken; }
            }

            private readonly string _oAuthToken;
        }

        private class Failure: State
        {
            public Failure(string reason)
            {
                _reason = reason;
            }

            public override string Result
            {
                get { return _reason; }
            }

            private readonly string _reason;
        }

        private class WaitForEmail: State
        {
            public override State Advance(TwoFactorAuth owner)
            {
                var validAnswers = new[] {Gui.Answer.Check, Gui.Answer.Resend};
                var answer = owner._gui.AskToWaitForEmail(owner._settings.Email, validAnswers);
                switch (answer)
                {
                case Gui.Answer.Check:
                    return Check(owner);
                case Gui.Answer.Resend:
                    Remote.AuthSendEmail(owner._clientInfo,
                                         owner._settings.Email,
                                         owner._settings.TransactionId,
                                         owner._http);
                    return this;
                }

                throw new InvalidOperationException(string.Format("Invalid answer '{0}'", answer));
            }
        }

        private class WaitForOob: State
        {
            public WaitForOob(int deviceIndex)
            {
                _deviceIndex = deviceIndex;
            }

            public override State Advance(TwoFactorAuth owner)
            {
                var validAnswers = new[] {Gui.Answer.Check, Gui.Answer.Resend, Gui.Answer.Email};
                var answer = owner._gui.AskToWaitForOob(owner._settings.Devices[_deviceIndex].Name,
                                                   owner._settings.Email,
                                                   validAnswers);
                switch (answer)
                {
                case Gui.Answer.Check:
                    return Check(owner);
                case Gui.Answer.Resend:
                    Remote.AuthSendPush(owner._clientInfo,
                                        owner._settings.Devices[_deviceIndex].Id,
                                        owner._settings.TransactionId,
                                        owner._http);
                    return this;
                case Gui.Answer.Email:
                    Remote.AuthSendEmail(owner._clientInfo,
                                         owner._settings.Email,
                                         owner._settings.TransactionId,
                                         owner._http);
                    return new WaitForEmail();
                }

                throw new InvalidOperationException(string.Format("Invalid answer '{0}'", answer));
            }

            private readonly int _deviceIndex;
        }

        private class ChooseOob: State
        {
            public override State Advance(TwoFactorAuth owner)
            {
                var names = owner._settings.Devices.Select(i => i.Name).ToArray();
                var validAnswers = Enumerable.Range(0, owner._settings.Devices.Length)
                    .Select(i => Gui.Answer.Device0 + i)
                    .Concat(new[] { Gui.Answer.Email })
                    .ToArray();
                var answer = owner._gui.AskToChooseOob(names, owner._settings.Email, validAnswers);

                if (answer == Gui.Answer.Email)
                {
                    Remote.AuthSendEmail(owner._clientInfo,
                                         owner._settings.Email,
                                         owner._settings.TransactionId,
                                         owner._http);
                    return new WaitForEmail();
                }

                var deviceIndex = answer - Gui.Answer.Device0;
                if (deviceIndex >= 0 && deviceIndex < owner._settings.Devices.Length)
                {
                    Remote.AuthSendPush(owner._clientInfo,
                                        owner._settings.Devices[deviceIndex].Id,
                                        owner._settings.TransactionId,
                                        owner._http);
                    return new WaitForOob(deviceIndex);
                }

                throw new InvalidOperationException(string.Format("Invalid answer '{0}'", answer));
            }
        }

        private TwoFactorAuth(Remote.ClientInfo clientInfo, Settings settings, Gui gui, IHttpClient http)
        {
            _clientInfo = clientInfo;
            _settings = settings;
            _gui = gui;
            _http = http;
        }

        private string Run(Step nextStep)
        {
            var state = CreateInitialState(nextStep);
            while (!state.IsDone)
                state = state.Advance(this);

            if (state.IsSuccess)
                return state.Result;

            throw new InvalidOperationException(string.Format("Two step verification failed: {0}",
                                                              state.Result));
        }

        private State CreateInitialState(Step step)
        {
            switch (step)
            {
            case Step.Done:
                return new Done(_settings.OAuthToken);
            case Step.WaitForEmail:
                return new WaitForEmail();
            case Step.WaitForOob:
                return _settings.Devices.Length > 1
                    ? CreateInitialState(Step.ChooseOob)
                    : new WaitForOob(0);
            case Step.ChooseOob:
                return new ChooseOob();
            }

            throw new InvalidOperationException(string.Format("Two factor auth step {0} is not supported", step));
        }

        private readonly Remote.ClientInfo _clientInfo;
        private readonly Settings _settings;
        private readonly Gui _gui;
        private readonly IHttpClient _http;
    }
}
