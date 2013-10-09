using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// A timer class that delays property-change notifications until the application is idle
    /// </summary>
    public sealed class IdleDelayTimer : FrameworkElement
    {
        public static readonly DependencyProperty OutputProperty = DependencyProperty.Register(
            "Output",
            typeof(object),
            typeof(IdleDelayTimer));

        public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
            "Input",
            typeof(object),
            typeof(IdleDelayTimer),
            new PropertyMetadata(Input_PropertyChanged));

        public IdleDelayTimer(Binding source)
        {
            this.Source = source;

            this.SetBinding(InputProperty, this.Source);

            this.Delay = TimeSpan.FromMilliseconds(500);
        }

        public object Output
        {
            get
            {
                return this.GetValue(OutputProperty);
            }

            set
            {
                this.SetValue(OutputProperty, value);
            }
        }

        public object Input
        {
            get
            {
                return (string)this.GetValue(InputProperty);
            }

            set
            {
                this.SetValue(InputProperty, value);
            }
        }

        private BindingBase Source
        {
            get;
            set;
        }

        /// <summary>
        /// The delay between the input and output, defaults to 500 ms
        /// </summary>
        public TimeSpan Delay
        {
            get;
            set;
        }

        private DateTime? QueueTime
        {
            get;
            set;
        }

        private DispatcherTimer Timer
        {
            get;
            set;
        }

        private void OnInputChanged()
        {
            this.QueueTime = DateTime.UtcNow;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (this.QueueTime != null)
            {
                if (DateTime.UtcNow > (this.QueueTime + this.Delay))
                {
                    // propegate the value
                    this.QueueTime = null;
                    this.Output = this.Input;
                }
            }
        }

        private static void Input_PropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var adapter = sender as IdleDelayTimer;
            if (adapter != null)
            {
                adapter.OnInputChanged();
            }
        }

        public void Start()
        {
            if (this.Timer == null)
            {
                this.Timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
                this.Timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
                this.Timer.Tick += new EventHandler(Timer_Tick);
                this.Timer.Start();
            }
        }

        public void Stop()
        {
            if (this.Timer != null)
            {
                this.Timer.Stop();
                this.Timer.Tick -= Timer_Tick;                
                this.Timer = null;
            }
        }
    }
}
