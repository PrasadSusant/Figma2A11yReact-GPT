using System.ComponentModel;

namespace FigmaReader
{
    public class BaseModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged  

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

    }
}
