using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net;
using System.IO;
using Microsoft.Windows.Controls.Ribbon;
using System.Windows.Threading;
using System.Collections;

namespace DLProg
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, GUIIface
    {
        Context c;

        public Context Context { get { return c; } }

        public MainWindow()
        {
            c = new Context(Dispatcher,this);
            InitializeComponent();
            InitBrowser();
        }

        public Dispatcher GetDispatcher()
        {
            return Dispatcher;
        }

        void browser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            browserPath.Text = e.Uri.LocalPath;
        }

        public void PrintLog(String txt)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    logBox.AppendText(txt);
                    logBox.AppendText(System.Environment.NewLine);
                }));
        }

        private void upButton_Click(object sender, RoutedEventArgs e)
        {
            browser.Navigate(System.IO.Path.Combine(browser.Source.LocalPath,".."));
        }

        public void InitBrowser()
        {
            browser.Navigating += new NavigatingCancelEventHandler(browser_Navigating);
            try
            {
                if (!c.IsProjectLoaded)
                    return;

                browser.Navigate(new Uri(c.Project.RootDirectory));
            }
            catch (InvalidOperationException err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ResetBrowser()
        {
            try
            {
                browser.Navigate(new Uri(c.Project.RootDirectory));
            }
            catch (InvalidOperationException err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (!c.IsProjectLoaded)
                    return;

                c.Workers.Stop();
                c.SaveProject();
                Properties.Settings.Default.Save();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                e.CanExecute = !c.IsProjectLoaded || !c.Workers.IsRunning;
            }
            catch (InvalidOperationException err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            /*if (p)
            {
                // Ask if user wants to save 
                MessageBox.Show(this, "Do you want to save changes?", "Save?", MessageBoxButton.YesNo);
            }
            */
            
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = true;
                fbd.Description = "";
                System.Windows.Forms.DialogResult r = fbd.ShowDialog();
                if (r != System.Windows.Forms.DialogResult.Cancel)
                {
                    Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                    sfd.DefaultExt = Properties.Resources.SaveFileDialog_DefaultExt;
                    sfd.Filter = Properties.Resources.SaveFileDialog_Filter;
                    sfd.Title = Properties.Resources.SaveFileDialog_Title;
                    if(sfd.ShowDialog(this) != true)
                        return;

                    c.NewProject(sfd.FileName, fbd.SelectedPath);
                }
            }
            
            ResetBrowser();
            e.Handled = true;
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                e.CanExecute = !c.IsProjectLoaded || !c.Workers.IsRunning;
            }
            catch (InvalidOperationException err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.DefaultExt = Properties.Resources.OpenFileDialog_DefaultExt;
            ofd.Filter = Properties.Resources.OpenFileDialog_Filter;
            ofd.Title = Properties.Resources.OpenFileDialog_Title;
            
            if (ofd.ShowDialog(this) != true)
                return;

            c.LoadProject(ofd.FileName);
            ResetBrowser();
            e.Handled = true;
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            }
            catch (InvalidOperationException err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            c.SaveProject();
            e.Handled = true;
        }

        private void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            }
            catch (InvalidOperationException err)
            {
                MessageBox.Show(err.Message, Properties.Resources.Errors_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.DefaultExt = Properties.Resources.SaveFileDialog_DefaultExt;
            sfd.Filter = Properties.Resources.SaveFileDialog_Filter;
            sfd.Title = Properties.Resources.SaveFileDialog_Title;
            if (sfd.ShowDialog(this) != true)
                return;

            c.SaveProjectAs(sfd.FileName);
            e.Handled = true;
        }

        private void Start_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning && c.Project.StartingUris.Count != 0;
            e.Handled = true;
        }

        private void Start_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            c.SaveProject();
            c.Workers.Start();
            e.Handled = true;
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && c.Workers.IsRunning;
            e.Handled = true;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            c.Workers.Stop();
            e.Handled = true;
        }

        private void ClearQueue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void ClearQueue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            c.Project.Links.Clear();
            e.Handled = true;
        }

        private void RequeueErrors_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void RequeueErrors_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            c.Project.RetryBadLinks();
            e.Handled = true;
        }

        private void ClearErrors_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void ClearErrors_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            c.Project.BadLinks.Clear();
            e.Handled = true;
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void DoDelete<T>(ObservableCollection<T> model, ListView view)
        {
            List<T> l = new List<T>(badLinks.SelectedItems.Cast<T>());
            foreach (T lk in l)
            {
                model.Remove(lk);
            }
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender == badLinks)
            {
                DoDelete<Link>(c.Project.BadLinks, badLinks);
                e.Handled = true;
                return;
            }
            else if (sender == links)
            {
                DoDelete<Link>(c.Project.Links, links);
                e.Handled = true;
                return;
            }
            else if (sender == startingUris)
            {
                DoDelete<Uri>(c.Project.StartingUris, startingUris);
                e.Handled = true;
                return;
            }
        }

        private void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender == badLinks)
            {
                List<Link> l = new List<Link>(badLinks.SelectedItems.Cast<Link>());
                Clipboard.SetData(DataFormats.Serializable, l);
                foreach (Link lk in l)
                {
                    c.Project.BadLinks.Remove(lk);
                }
                e.Handled = true;
                return;
            }
            else if (sender == links)
            {
                List<Link> l = new List<Link>(links.SelectedItems.Cast<Link>());
                Clipboard.SetData(DataFormats.Serializable, l);
                foreach (Link lk in l)
                {
                    c.Project.Links.Remove(lk);
                }
                e.Handled = true;
                return;
            }
        }

        private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender == badLinks)
            {
                List<Link> l = new List<Link>(badLinks.SelectedItems.Cast<Link>());
                Clipboard.SetData(DataFormats.Serializable, l);
                e.Handled = true;
                return;
            }
            else if (sender == links)
            {
                List<Link> l = new List<Link>(links.SelectedItems.Cast<Link>());
                Clipboard.SetData(DataFormats.Serializable, l);
                e.Handled = true;
                return;
            }
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = c.IsProjectLoaded && !c.Workers.IsRunning;
            e.Handled = true;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender == badLinks)
            {
                List<Link> l = Clipboard.GetData(DataFormats.Serializable) as List<Link>;
                if (l == null)
                    return;
                foreach (Link lk in l)
                {
                    c.Project.BadLinks.Add(lk);
                }
                e.Handled = true;
                return;
            }
            else if (sender == links)
            {
                List<Link> l = Clipboard.GetData(DataFormats.Serializable) as List<Link>;
                if (l == null)
                    return;
                foreach (Link lk in l)
                {
                    c.Project.Links.Add(lk);
                }
                e.Handled = true;
                return;
            }
        }

        private void badLinks_MouseUp(object sender, MouseButtonEventArgs e)
        {
            badLinks.Focus();
        }

        private void links_MouseUp(object sender, MouseButtonEventArgs e)
        {
            links.Focus();
        }

        private void RibbonApplicationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            (new OptionsWindow()).ShowDialog();
        }
    }
}
