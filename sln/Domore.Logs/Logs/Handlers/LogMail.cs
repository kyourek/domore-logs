using System;
using System.Net.Mail;

namespace Domore.Logs.Handlers {
    class LogMail : LogHandler.Background {
        MailService _Mail;
        internal MailService Mail {
            get => _Mail ?? (_Mail = new MailService());
            set => Change(ref _Mail, value, nameof(Mail));
        }

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

        string _Subject;
        public string Subject {
            get => _Subject ?? (_Subject = "[Domore.Logs LogMail] " + Name);
            set => Change(ref _Subject, value, nameof(Subject));
        }

        string _Host;
        public string Host {
            get => _Host;
            set => Change(ref _Host, value, nameof(Host));
        }

        int _Port = 25;
        public int Port {
            get => _Port;
            set => Change(ref _Port, value, nameof(Port));
        }

        string _FromAddress;
        public string FromAddress {
            get => _FromAddress;
            set => Change(ref _FromAddress, value, nameof(FromAddress));
        }

        string _FromDisplayName;
        public string FromDisplayName {
            get => _FromDisplayName ?? (_FromDisplayName = "LogMail");
            set => Change(ref _FromDisplayName, value, nameof(FromDisplayName));
        }

        string _To;
        public string To {
            get => _To;
            set => Change(ref _To, value, nameof(To));
        }

        internal class MailService {
            Action<SmtpClient, MailMessage> _Send;
            public Action<SmtpClient, MailMessage> Send {
                get => _Send ?? (_Send = (smtp, mail) => smtp.Send(mail));
                set => _Send = value;
            }
        }
    }
}
