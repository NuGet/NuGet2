using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace NuGet.WebMatrix
{
    internal class ButtonBarViewModel
    {
        public ButtonBarViewModel(NuGetViewModel nuGetViewModel, ICommand closeCommand)
        {
            this.PropertyNames = new List<string>()
            {
                "Loading",
                "SelectedPackage",
                "IsLicensePageVisible",
                "IsDetailsPaneVisible",
                "IsUninstallPageVisible",
            };

            this.ActionButtons = new ObservableCollection<ButtonViewModel>();

            this.NuGetViewModel = nuGetViewModel;
            
            this.CloseCommand = closeCommand;

            this.AcceptButton = new ButtonViewModel()
            {
                // the command assignment here is dynamic (might be update or install)
                Text = Resources.String_I__Accept,
                InvariantName = "acceptButton",
            };

            this.DeclineButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.ShowListCommand,
                Text = Resources.String_I__Decline,
                InvariantName = "declineButton",
                IsCancel = true,
            };

            this.InstallButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.ShowDetailsPageCommand,
                CommandParameter = PackageViewModelAction.InstallOrUninstall,
                Text = Resources.String__Install,
                InvariantName = "installButton",
            };

            this.UninstallButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.ShowUninstallPageCommand,
                Text = Resources.String__Uninstall,
                InvariantName = "uninstallButton",
            };

            this.UpdateButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.ShowDetailsPageCommand,
                CommandParameter = PackageViewModelAction.Update,
                Text = Resources.String__Update,
                InvariantName = "updateButton",
            };

            this.UpdateAllButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.ShowLicensePageForAllCommand,
                CommandParameter = PackageViewModelAction.UpdateAll,
                Text = Resources.String__Update_All,
                InvariantName = "updateAllButton",
            };

            this.YesButton = new ButtonViewModel()
            {
                // this is dynamic:
                // - from the details page, we want to go to the license page
                // - from the uninstall page, we just want to uninstall
                Text = Resources.String__Yes,
                InvariantName = "yesButton",
            };

            this.NoButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.ShowListCommand,
                Text = Resources.String__No,
                InvariantName = "noButton",
                IsCancel = true,
            };

            this.CloseButton = new ButtonViewModel()
            {
                Command = this.CloseCommand,
                Text = Resources.String__Close,
                InvariantName = "closeButton",
                IsCancel = true,
            };

            this.EnableButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.EnableCommand,
                Text = Resources.String__Enable,
                InvariantName = "enableButton",
            };

            this.DisableButton = new ButtonViewModel()
            {
                Command = this.NuGetViewModel.DisableCommand,
                Text = Resources.String__Disable,
                InvariantName = "disableButton",
            };

            this.NuGetViewModel.PropertyChanged += Gallery_PropertyChanged;

            this.PrereleaseFilter = new List<string>()
            {
                Resources.Prerelease_Filter_StableOnly,
                Resources.Prerelease_Filter_IncludePrerelease,                
            };

            this.Refresh();
        }

        public ICommand CloseCommand
        {
            get;
            private set;
        }

        public NuGetViewModel NuGetViewModel
        {
            get;
            private set;
        }

        public ObservableCollection<ButtonViewModel> ActionButtons
        {
            get;
            private set;
        }

        public List<string> PrereleaseFilter
        {
            get;
            set;
        }

        private List<string> PropertyNames
        {
            get;
            set;
        }

        // This is just a private helper function to set visibility of the comboboxes (FeedSource and IncludePrerelease)
        private void SetComboBoxesVisibility(bool showPrereleaseFilter, bool showFeedSourceCombobox)
        {
            this.NuGetViewModel.ShowPrereleaseFilter = showPrereleaseFilter && this.NuGetViewModel.ShouldShowPrereleaseFilter ? Visibility.Visible : Visibility.Collapsed;

            this.NuGetViewModel.ShowFeedSourceComboBox = showFeedSourceCombobox && this.NuGetViewModel.ShouldShowFeedSource ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Refresh()
        {
            this.ActionButtons.Clear();

            SetComboBoxesVisibility(false, false);

            if (this.NuGetViewModel.Loading)
            {
                SetComboBoxesVisibility(true, true);
                this.ActionButtons.Add(this.CloseButton);
            }
            else if (this.NuGetViewModel.IsLicensePageVisible)
            {
                switch (this.NuGetViewModel.PackageAction)
                {
                    case PackageViewModelAction.InstallOrUninstall:
                        this.AcceptButton.Command = this.NuGetViewModel.InstallCommand;
                        break;

                    case PackageViewModelAction.Update:
                        this.AcceptButton.Command = this.NuGetViewModel.UpdateCommand;
                        break;

                    case PackageViewModelAction.UpdateAll:
                        this.AcceptButton.Command = this.NuGetViewModel.UpdateAllCommand;
                        break;
                }

                this.ActionButtons.Add(this.AcceptButton);
                this.ActionButtons.Add(this.DeclineButton);
            }
            else if (this.NuGetViewModel.IsDetailsPaneVisible)
            {
                this.YesButton.Command = this.NuGetViewModel.ShowLicensePageCommand;
                this.ActionButtons.Add(this.YesButton);
                this.ActionButtons.Add(this.NoButton);
            }
            else if (this.NuGetViewModel.IsUninstallPageVisible)
            {
                this.YesButton.Command = this.NuGetViewModel.UninstallCommand;
                this.ActionButtons.Add(this.YesButton);
                this.ActionButtons.Add(this.NoButton);
            }
            else if (this.NuGetViewModel.SelectedPackage == null)
            {
                SetComboBoxesVisibility(true, true);
                this.ActionButtons.Add(this.CloseButton);
            }
            else
            {
                SetComboBoxesVisibility(true, true);

                if (this.NuGetViewModel.ShowUpdateAll)
                {
                    this.ActionButtons.Add(this.UpdateAllButton);
                }

                // there is a package -- determine which buttons to show
                var selectedPackage = this.NuGetViewModel.SelectedPackage;
                if (selectedPackage.IsInstalled)
                {
                    // Note that the Disable Button will appear disabled for mandatory extensions
                    if (selectedPackage.SupportsEnableDisable)
                    {
                        if (selectedPackage.IsEnabled)
                        {
                            this.ActionButtons.Add(this.DisableButton);
                        }
                        else
                        {
                            this.ActionButtons.Add(this.EnableButton);
                        }
                    }

                    if (selectedPackage.HasUpdates)
                    {
                        this.ActionButtons.Add(this.UpdateButton);
                    }

                    // Note that the Uninstall Button will appear disabled for mandatory extensions
                    this.ActionButtons.Add(this.UninstallButton);
                    this.ActionButtons.Add(this.CloseButton);
                }
                else
                {
                    this.ActionButtons.Add(this.InstallButton);
                    this.ActionButtons.Add(this.CloseButton);
                }
            }
        }

        private void Gallery_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.PropertyNames.Contains(e.PropertyName))
            {
                this.Refresh();
            }
        }

        internal ButtonViewModel AcceptButton
        {
            get;
            private set;
        }

        internal ButtonViewModel DeclineButton
        {
            get;
            private set;
        }

        internal ButtonViewModel YesButton
        {
            get;
            private set;
        }

        internal ButtonViewModel NoButton
        {
            get;
            private set;
        }

        internal ButtonViewModel InstallButton
        {
            get;
            private set;
        }

        internal ButtonViewModel UninstallButton
        {
            get;
            private set;
        }

        internal ButtonViewModel UpdateButton
        {
            get;
            private set;
        }

        internal ButtonViewModel UpdateAllButton
        {
            get;
            private set;
        }

        internal ButtonViewModel CloseButton
        {
            get;
            private set;
        }

        internal ButtonViewModel EnableButton
        {
            get;
            private set;
        }

        internal ButtonViewModel DisableButton
        {
            get;
            private set;
        }
    }
}
