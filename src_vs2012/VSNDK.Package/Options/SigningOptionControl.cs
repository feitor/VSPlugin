﻿using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Options.Dialogs;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    public partial class SigningOptionControl : UserControl
    {
        private readonly SigningOptionViewModel _vm = new SigningOptionViewModel();

        public SigningOptionControl()
        {
            InitializeComponent();
            UpdateUI();
        }

        #region Properties

        public string CertificatePath
        {
            get { return txtCertPath.Text; }
            set { txtCertPath.Text = value; }
        }

        public string Author
        {
            get { return txtAuthor.Text; }
            set { txtAuthor.Text = value; }
        }

        #endregion

        private void UpdateUI()
        {
            CertificatePath = _vm.Developer.CertificateFileName;
            Author = _vm.Developer.Name;

            //bttRegister.Enabled = !_vm.Developer.IsRegistered;
            //bttUnregister.Enabled = !bttRegister.Enabled;
            //bttBackup.Enabled = bttUnregister.Enabled;
            lblStatus.Text = _vm.Developer.ToShortStatusDescription();

            bttNavigate.Enabled = !string.IsNullOrEmpty(CertificatePath);
            bttDeletePassword.Enabled = _vm.Developer.IsPasswordSaved;
            bttRefresh.Enabled = bttNavigate.Enabled;
        }

        /// <summary>
        /// Control has become visible again, when user navigated over Settings.
        /// </summary>
        public void OnActivate()
        {
            UpdateUI();
        }

        private void ReloadAuthor()
        {
            if (CertHelper.ReloadAuthor(_vm.Developer))
            {
                UpdateUI();
            }
        }

        private void lblMore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL("http://www.blackberry.com/go/codesigning/");
        }

        private void bttChangeCert_Click(object sender, EventArgs e)
        {
            CertHelper.Import(_vm.Developer);
            UpdateUI();
        }

        private void bttNavigate_Click(object sender, EventArgs e)
        {
            var form = new CertificateDetailsForm(_vm.Developer);
            form.ShowDialog();

            UpdateUI();
        }

        private void bttDeletePassword_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Are you sure to delete stored certificate password?\r\n\r\nYou will need to type it in, whenever required.",
                null, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _vm.Developer.ClearPassword();
            UpdateUI();
        }

        private void bttRefresh_Click(object sender, EventArgs e)
        {
            ReloadAuthor();
        }

        private void bttBackup_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.SaveZipFile("Exporting Developer Profile", "profile_backup_" + DateTime.Now.ToString("yyyy-MM-dd") + ".zip");

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (_vm.Developer.BackupProfile(form.FileName))
                {
                    MessageBoxHelper.Show("Developer profile exported", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBoxHelper.Show("Error while exporting developer profile", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                UpdateUI();
            }
        }

        private void bttRestore_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.OpenZipFile("Restoring Developer Profile");

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (_vm.Developer.RestoreProfile(form.FileName))
                {
                    MessageBoxHelper.Show("Developer profile restored", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBoxHelper.Show("Error while importing developer profile", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                ReloadAuthor();
            }
        }

        private void bttUnregister_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Do you want to unregister and remove the BlackBerry ID Token file?\r\nThis operation can not be reverted. Make sure you have created a backup.", "UNREGISTRATION!!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ///////////////////////
                // UNREGISTER
                // this is the only place, where doing it synchronously is OK:
                var runner = new KeyToolRemoveRunner(RunnerDefaults.ToolsDirectory);
                var success = runner.Execute();

                // and delete all profile-related files:
                _vm.Developer.DeleteProfile();
                UpdateUI();

                // finally, show result message:
                if (success && string.IsNullOrEmpty(runner.LastError))
                {
                    if (!string.IsNullOrEmpty(runner.LastOutput))
                    {
                        MessageBoxHelper.Show(runner.LastOutput.Replace("CSK", "BlackBerry ID token"), "Unregistered developer profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(runner.LastError))
                    {
                        MessageBoxHelper.Show(runner.LastError, "Failed to remove developer profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void bttRegister_Click(object sender, EventArgs e)
        {
            /*
            if (_vm.Developer.IsRegistered)
            {
                MessageBoxHelper.Show("You are already registered, please unregister first, if you want to create new BlackBerry ID token!", "Registration",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
             */

            ///////////////////////////////
            // REGISTER

            var registrationForm = new RegistrationForm(_vm.Developer, 0);
            registrationForm.ShowDialog();

            UpdateUI();
        }
    }
}
