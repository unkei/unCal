﻿using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using System.Diagnostics;
using Microsoft.Phone.Shell;
using System.Linq;
using libWkCal;

namespace TileUpdate
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;
        private WeekCalendar wc = new WeekCalendar();

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            Debug.WriteLine("Tile Update bg-agent invoked.");

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=unCal"));

                if (TileToFind != null)
                {
                    wc.createCalendarImage("Shared\\ShellContent\\unCal.jpg", DateTime.Now, true, true);

                    StandardTileData tileData = new StandardTileData
                    {
                        BackgroundImage = new Uri("isostore:/Shared/ShellContent/unCal.jpg", UriKind.Absolute),
                    };
                    TileToFind.Update(tileData);
                }
            });

            NotifyComplete();
        }
    }
}