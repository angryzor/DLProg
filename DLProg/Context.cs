using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.IO;
using System.ComponentModel;

namespace DLProg
{
    public class Context : INotifyPropertyChanged
    {
        private Project project = null;
        private Workers workers = null;
        private String currentProjectFile = null;
        private GUIIface gui;

        public Project Project { get { 
            if (!IsProjectLoaded) 
                throw new InvalidOperationException("No project loaded"); 
            return project; } }
        public Workers Workers { get { 
            if (!IsProjectLoaded) 
                throw new InvalidOperationException("No project loaded"); 
            return workers; } }

        public String CurrentProjectFile { get { return currentProjectFile; } }
        public bool IsProjectLoaded { get { return project != null; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public Context(Dispatcher d, GUIIface gui)
        {
            this.gui = gui;
            //NewProject(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        }

        public void NewProject(String projectFile, String rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
                Directory.CreateDirectory(rootDirectory);

            SetProject(new Project(gui, rootDirectory));
            currentProjectFile = projectFile;
            NotifyPropertyChanged("CurrentProjectFile");
        }

        public void LoadProject(String projectFile)
        {
            SetProject(Project.Load(gui, projectFile));
            currentProjectFile = projectFile;
            NotifyPropertyChanged("CurrentProjectFile");
        }

        public void SaveProject()
        {
            project.Save(currentProjectFile);
        }

        public void SaveProjectAs(string newFile)
        {
            currentProjectFile = newFile;
            SaveProject();
        }


        private void SetProject(Project p)
        {
            if (workers != null && workers.IsRunning)
                throw new InvalidOperationException("Cannot load a new project into the program context while the current project is running.");
            if (IsProjectLoaded)
                SaveProject();

            project = p;
            workers = new Workers(project, gui);
            NotifyPropertyChanged("IsProjectLoaded");
            NotifyPropertyChanged("Project");
            NotifyPropertyChanged("Workers");
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
