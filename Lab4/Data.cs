using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab4
{
    class Data: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private double temperature;
        private int intensity;
        public string Temperature
        {
            get
            {
                return string.Format("{0:N2}℃", temperature);
            }
            set
            {
                temperature = getTemp(int.Parse(value));
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Temperature"));
                }
            }
        }
        public string Intensity
        {
            get
            {
                return intensity.ToString()+"℉";
            }
            set
            {
                intensity = int.Parse(value);
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Intensity"));
                }
            }
        }
        private double getTemp(int ntc)
        {
            double R0 = 1e4, R5 = 1e4, B = 3435.0, T0 = 298.15;
            double Rth = R5 * (1024.0/ntc - 1.0);
            double T = 1.0/(Math.Log(Rth/R0) /B + 1/T0) - 273.15;
            return T;
        }
        public double Get_Temp()
        {
            return temperature;
        }
        public double Get_Inte()
        {
            return intensity;
        }
    }
}
