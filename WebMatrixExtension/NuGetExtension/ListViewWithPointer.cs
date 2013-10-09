using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NuGet.WebMatrix
{
    internal class ListViewWithPointer : ListView
    {
        private ItemsPresenter m_ItemsPresenter;
        private FrameworkElement m_Pointer;
        private TranslateTransform m_Translation;
        private Storyboard m_Storyboard;
        private DoubleAnimation m_Animation;

        public ListViewWithPointer()
        {
            m_Storyboard = new Storyboard();
            m_Animation = new DoubleAnimation();

            // IMPORTANT: AccelerationRatio+DecelerationRatio must sum to a value <= 1
            // However, on some machines the value 0.1+0.9 reports as being > 1 due to
            // rounding problems. Therefore, use two values that sum to a value below
            // 1 to avoid the exception on a small fraction of customer machines.
            m_Animation.AccelerationRatio = 0.1;
            m_Animation.DecelerationRatio = 0.89;
            m_Animation.Duration = new Duration(TimeSpan.FromMilliseconds(100));
            m_Storyboard.Children.Add(m_Animation);

            Loaded += new RoutedEventHandler(OnLoaded);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AnimateToSelectedIndex();
        }

        public override void OnApplyTemplate()
        {
            m_ItemsPresenter = GetTemplateChild("ItemsPresenter") as ItemsPresenter;
            m_Pointer = GetTemplateChild("Pointer") as FrameworkElement;
            if (m_Pointer != null)
            {
                m_Pointer.RenderTransform = m_Translation = new TranslateTransform();

                Storyboard.SetTarget(m_Animation, m_Pointer);
                Storyboard.SetTargetProperty(m_Animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            }

            base.OnApplyTemplate();
            AnimateToSelectedIndex();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            AnimateToSelectedIndex();
            base.OnSelectionChanged(e);
        }

        private void AnimateToSelectedIndex()
        {
            if (m_ItemsPresenter != null && m_Pointer != null)
            {
                if (SelectedIndex >= 0)
                {
                    try
                    {
                        Panel panel = (VisualTreeHelper.GetChildrenCount(m_ItemsPresenter) > 0)
                            ? VisualTreeHelper.GetChild(m_ItemsPresenter, 0) as Panel
                            : null;

                        if (panel != null
                            && panel.Children != null
                            && panel.Children.Count > SelectedIndex)
                        {
                            FrameworkElement element = (FrameworkElement)panel.Children[SelectedIndex];

                            m_Animation.To = ((MatrixTransform)element.TransformToVisual(this)).Matrix.OffsetY + (element.ActualHeight / 2.0f) - (m_Pointer.ActualHeight / 2.0f);
                            m_Storyboard.Begin(m_Pointer);
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }
        }
    }
}
