﻿// FFXIVAPP.Client
// TargetWorker.cs
// 
// © 2013 Ryan Wilson

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using FFXIVAPP.Client.Delegates;
using FFXIVAPP.Client.Helpers;
using FFXIVAPP.Common.Core.Memory;
using NLog;
using SmartAssembly.Attributes;

namespace FFXIVAPP.Client.Memory
{
    [DoNotObfuscate]
    internal class TargetWorker : INotifyPropertyChanged, IDisposable
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Property Bindings

        private TargetEntity LastTargetEntity { get; set; }

        #endregion

        #region Declarations

        private readonly Timer _scanTimer;
        private bool _isScanning;

        #endregion

        public TargetWorker()
        {
            _scanTimer = new Timer(100);
            _scanTimer.Elapsed += ScanTimerElapsed;
        }

        #region Timer Controls

        /// <summary>
        /// </summary>
        public void StartScanning()
        {
            _scanTimer.Enabled = true;
        }

        /// <summary>
        /// </summary>
        public void StopScanning()
        {
            _scanTimer.Enabled = false;
        }

        #endregion

        #region Threads

        /// <summary>
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="e"> </param>
        private void ScanTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isScanning)
            {
                return;
            }
            _isScanning = true;
            Func<bool> scannerWorker = delegate
            {
                if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("GAMEMAIN"))
                {
                    if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("CHARMAP"))
                    {
                        try
                        {
                            var targetHateStructure = MemoryHandler.Instance.SigScanner.Locations["CHARMAP"] + 1136;
                            var enmityEntries = new List<EnmityEntry>();
                            var targetEntity = new TargetEntity();
                            if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("TARGET"))
                            {
                                var targetAddress = MemoryHandler.Instance.SigScanner.Locations["TARGET"];
                                var somethingFound = false;
                                if (targetAddress > 0)
                                {
                                    var targetInfo = MemoryHandler.Instance.GetStructure<Structures.Target>(targetAddress);
                                    if (targetInfo.CurrentTarget > 0)
                                    {
                                        try
                                        {
                                            var source = MemoryHandler.Instance.GetByteArray(targetInfo.CurrentTarget, 0x3F40);
                                            var entry = ActorEntityHelper.ResolveActorFromBytes(source);
                                            if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("MAP"))
                                            {
                                                try
                                                {
                                                    entry.MapIndex = MemoryHandler.Instance.GetUInt32(MemoryHandler.Instance.SigScanner.Locations["MAP"]);
                                                }
                                                catch (Exception ex)
                                                {
                                                }
                                            }
                                            if (entry.IsValid)
                                            {
                                                somethingFound = true;
                                                targetEntity.CurrentTarget = entry;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                    if (targetInfo.MouseOverTarget > 0)
                                    {
                                        try
                                        {
                                            var source = MemoryHandler.Instance.GetByteArray(targetInfo.MouseOverTarget, 0x3F40);
                                            var entry = ActorEntityHelper.ResolveActorFromBytes(source);
                                            if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("MAP"))
                                            {
                                                try
                                                {
                                                    entry.MapIndex = MemoryHandler.Instance.GetUInt32(MemoryHandler.Instance.SigScanner.Locations["MAP"]);
                                                }
                                                catch (Exception ex)
                                                {
                                                }
                                            }
                                            if (entry.IsValid)
                                            {
                                                somethingFound = true;
                                                targetEntity.MouseOverTarget = entry;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                    if (targetInfo.FocusTarget > 0)
                                    {
                                        var source = MemoryHandler.Instance.GetByteArray(targetInfo.FocusTarget, 0x3F40);
                                        var entry = ActorEntityHelper.ResolveActorFromBytes(source);
                                        if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("MAP"))
                                        {
                                            try
                                            {
                                                entry.MapIndex = MemoryHandler.Instance.GetUInt32(MemoryHandler.Instance.SigScanner.Locations["MAP"]);
                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                        }
                                        if (entry.IsValid)
                                        {
                                            somethingFound = true;
                                            targetEntity.FocusTarget = entry;
                                        }
                                    }
                                    if (targetInfo.PreviousTarget > 0)
                                    {
                                        try
                                        {
                                            var source = MemoryHandler.Instance.GetByteArray(targetInfo.PreviousTarget, 0x3F40);
                                            var entry = ActorEntityHelper.ResolveActorFromBytes(source);
                                            if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("MAP"))
                                            {
                                                try
                                                {
                                                    entry.MapIndex = MemoryHandler.Instance.GetUInt32(MemoryHandler.Instance.SigScanner.Locations["MAP"]);
                                                }
                                                catch (Exception ex)
                                                {
                                                }
                                            }
                                            if (entry.IsValid)
                                            {
                                                somethingFound = true;
                                                targetEntity.PreviousTarget = entry;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                    if (targetInfo.CurrentTargetID > 0)
                                    {
                                        somethingFound = true;
                                        targetEntity.CurrentTargetID = targetInfo.CurrentTargetID;
                                    }
                                }
                                if (targetEntity.CurrentTargetID > 0)
                                {
                                    for (uint i = 0; i < 16; i++)
                                    {
                                        var address = targetHateStructure + (i * 72);
                                        var enmityEntry = new EnmityEntry
                                        {
                                            ID = (uint)MemoryHandler.Instance.GetInt32(address),
                                            Enmity = (uint)MemoryHandler.Instance.GetInt32(address + 4)
                                        };
                                        if (enmityEntry.ID <= 0)
                                        {
                                            continue;
                                        }
                                        if (PCWorkerDelegate.GetUniqueNPCEntities()
                                                            .Any())
                                        {
                                            if (PCWorkerDelegate.GetUniqueNPCEntities()
                                                                .Any(a => a.ID == enmityEntry.ID))
                                            {
                                                enmityEntry.Name = PCWorkerDelegate.GetUniqueNPCEntities()
                                                                                   .First(a => a.ID == enmityEntry.ID)
                                                                                   .Name;
                                            }
                                        }
                                        if (String.IsNullOrWhiteSpace(enmityEntry.Name))
                                        {
                                            if (NPCWorkerDelegate.GetUniqueNPCEntities()
                                                                 .Any())
                                            {
                                                if (NPCWorkerDelegate.GetUniqueNPCEntities()
                                                                     .Any(a => a.ID == enmityEntry.ID))
                                                {
                                                    enmityEntry.Name = NPCWorkerDelegate.GetUniqueNPCEntities()
                                                                                        .First(a => a.NPCID2 == enmityEntry.ID)
                                                                                        .Name;
                                                }
                                            }
                                        }
                                        if (String.IsNullOrWhiteSpace(enmityEntry.Name))
                                        {
                                            if (MonsterWorkerDelegate.GetUniqueNPCEntities()
                                                                     .Any())
                                            {
                                                if (MonsterWorkerDelegate.GetUniqueNPCEntities()
                                                                         .Any(a => a.ID == enmityEntry.ID))
                                                {
                                                    enmityEntry.Name = MonsterWorkerDelegate.GetUniqueNPCEntities()
                                                                                            .First(a => a.ID == enmityEntry.ID)
                                                                                            .Name;
                                                }
                                            }
                                        }
                                        enmityEntries.Add(enmityEntry);
                                    }
                                }
                                targetEntity.EnmityEntries = enmityEntries;
                                if (somethingFound)
                                {
                                    AppContextHelper.Instance.RaiseNewTargetEntity(targetEntity);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                _isScanning = false;
                return true;
            };
            scannerWorker.BeginInvoke(delegate { }, scannerWorker);
        }

        #endregion

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            _scanTimer.Elapsed -= ScanTimerElapsed;
        }

        #endregion
    }
}