using System.ComponentModel;

namespace Domore.ComponentModel {
    public class NotifyPropertyChangedImplementation : INotifyPropertyChanged {
        protected void NotifyPropertyChanged(string propertyName) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void NotifyPropertyChanged() {
            NotifyPropertyChanged(string.Empty);
        }

        protected bool Change<T>(ref T field, T value, string propertyName) {
            if (Equals(field, value)) {
                return false;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChanged?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
