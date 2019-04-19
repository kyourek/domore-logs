using System.ComponentModel;

namespace Domore.ComponentModel {
    public class NotifyPropertyChangedImplementation : INotifyPropertyChanged {
        protected void NotifyPropertyChanged(string propertyName) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void NotifyPropertyChanged() {
            NotifyPropertyChanged(string.Empty);
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            var handler = PropertyChanged;
            if (handler != null) handler.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
