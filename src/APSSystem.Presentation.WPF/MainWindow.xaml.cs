using System.Collections.Generic;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace APSSystem.Presentation.WPF
{
    public partial class MainWindow : Window
    {
        public IEnumerable<ISeries> Series { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Ocupação (min)",
                    Values = new double[] { 120, 95, 180, 140, 200 }
                }
            };

            DataContext = this;
        }
    }
}
