using System;
using System.Net.Mail;

namespace Domore.Logs.Handlers {
    internal class LogMail : LogHandler.Background {
        internal MailService Mail {
            get { return _Mail ?? (_Mail = new MailService()); }
            set { _Mail = value; }
        }
        private MailService _Mail;

        protected override void HandleAction(string message) {
            var to = To;
            if (to != null && to.Trim() != "") {
                using (var mail = new MailMessage()) {
                    mail.Body = message;
                    mail.From = new MailAddress(FromAddress, FromDisplayName);
                    mail.Subject = Subject;
                    mail.To.Add(to);

                    using (var smtp = new SmtpClient()) {
                        smtp.Host = Host;
                        smtp.Port = Port;
                        Mail.Send(smtp, mail);
                    }
                }
            }
        }

        public string Subject {
            get { return _Subject ?? (_Subject = "[Domore.Logs LogMail] " + Name); }
            set { _Subject = value; }
        }
        private string _Subject;

        public string Host {
            get { return _Host; }
            set { _Host = value; }
        }
        private string _Host;

        public int Port {
            get { return _Port; }
            set { _Port = value; }
        }
        private int _Port = 25;

        public string FromAddress {
            get { return _FromAddress; }
            set { _FromAddress = value; }
        }
        private string _FromAddress;

        public string FromDisplayName {
            get { return _FromDisplayName ?? (_FromDisplayName = "LogMail"); }
            set { _FromDisplayName = value; }
        }
        private string _FromDisplayName;

        public string To {
            get { return _To; }
            set { _To = value; }
        }
        private string _To;

        internal class MailService {
            public Action<SmtpClient, MailMessage> Send {
                get { return _Send ?? (_Send = (smtp, mail) => smtp.Send(mail)); }
                set { _Send = value; }
            }
            private Action<SmtpClient, MailMessage> _Send;
        }
    }
}
