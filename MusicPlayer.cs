using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace IBUS_sniffer
{
    public class TrackChangedEventArgs
    {
        public byte Directory { get; set; }
        public byte File { get; set; }

        public TrackChangedEventArgs(byte dir, byte filenum)
        {
            Directory = dir;
            File = filenum;
        }
    }

    public delegate void TrackChangedHandler(MusicPlayer sender, TrackChangedEventArgs e);

    public class MusicPlayer
    {
        Process _player;
        int _currentFileIndex = 0;
        int _currentDirectoryIndex = 0;
        List<string> _files;
        List<string> _directories;

        public byte CurrentDirectory { get { return (byte) (_currentDirectoryIndex + 1); } }
        public byte CurrentFile { get { return (byte)(_currentFileIndex + 1); } }

        public event TrackChangedHandler TrackChanged;

        public MusicPlayer()
        {
            _directories = Directory.EnumerateDirectories(ConfigurationManager.AppSettings["BaseDir"]).ToList();

            LoadFileList();
        }

        private void LoadFileList()
        {
            _files = Directory.EnumerateFiles(
                _directories[_currentDirectoryIndex], 
                "*.mp3", 
                SearchOption.AllDirectories).ToList();
        }

        public void NextDirectory()
        {
            _currentDirectoryIndex++;
            _currentFileIndex = 0;

            if (_currentDirectoryIndex >= _directories.Count)
            {
                _currentDirectoryIndex = 0;
            }

            LoadFileList();
            StartPlayer();
        }

        public void PreviousDirectory()
        {
            _currentDirectoryIndex--;
            _currentFileIndex = 0;

            if (_currentDirectoryIndex < 0)
            {
                _currentDirectoryIndex = _directories.Count - 1;
            }

            LoadFileList();
            StartPlayer();
        }

        public void NextFile()
        {
            _currentFileIndex++;

            if (_currentFileIndex >= _files.Count)
            {
                //_currentFileIndex = 0;
                NextDirectory();
            }
            else
            {
                StartPlayer();
            }
        }

        public void PreviousFile()
        {
            _currentFileIndex--;

            if (_currentFileIndex < 0)
            {
                //_currentFileIndex = _files.Count - 1;

                PreviousDirectory();
            }
            else
            {
                StartPlayer();
            }
        }

        public void StartPlayer()
        {
            StopPlayer();

            _player = new Process();
            _player.StartInfo = new ProcessStartInfo
                {
                    FileName = ConfigurationManager.AppSettings["PlayProgram"], 
                    Arguments = "\"" + _files[_currentFileIndex] + "\"", 
                    UseShellExecute = true
                };
            _player.EnableRaisingEvents = true;
            _player.Exited += new EventHandler(_player_Exited);

            _player.Start();
        }

        private void _player_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("\n*** Process exit event captured\n");

            // Wrongly called?
            if (!_player.HasExited)
                return;

            Console.WriteLine("\n*** Finished, going to next song\n");

            NextFile();

            if (this.TrackChanged != null)
            {
                this.TrackChanged(this, new TrackChangedEventArgs(CurrentDirectory, CurrentFile));
            }

            StartPlayer();
        }

        public void StopPlayer()
        {
            if (_player != null && !_player.HasExited)
            {
                _player.Kill();
                _player.Close();
            }
        }
    }
}
