﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DehnadFtpSyncAndChargingService.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("E:\\ImiFtps\\MciDirectNew")]
        public string LocalPathMCI {
            get {
                return ((string)(this["LocalPathMCI"]));
            }
            set {
                this["LocalPathMCI"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("900")]
        public long DownloadIntervalInSeconds {
            get {
                return ((long)(this["DownloadIntervalInSeconds"]));
            }
            set {
                this["DownloadIntervalInSeconds"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool SaveFailFilesToSingleCharge {
            get {
                return ((bool)(this["SaveFailFilesToSingleCharge"]));
            }
            set {
                this["SaveFailFilesToSingleCharge"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int CheckNDaysBefore {
            get {
                return ((int)(this["CheckNDaysBefore"]));
            }
            set {
                this["CheckNDaysBefore"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("All")]
        public string SyncServices {
            get {
                return ((string)(this["SyncServices"]));
            }
            set {
                this["SyncServices"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1440")]
        public int SyncFtpWaitTimeInMins {
            get {
                return ((int)(this["SyncFtpWaitTimeInMins"]));
            }
            set {
                this["SyncFtpWaitTimeInMins"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("60")]
        public int SyncDBWaitTimeInMins {
            get {
                return ((int)(this["SyncDBWaitTimeInMins"]));
            }
            set {
                this["SyncDBWaitTimeInMins"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1440")]
        public int SyncFtpOldItemsInMins {
            get {
                return ((int)(this["SyncFtpOldItemsInMins"]));
            }
            set {
                this["SyncFtpOldItemsInMins"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("All (notif all services async) All:AutoSync(All services sync automatically) Taml" +
            "y500:AutoSync;Achar(Sync Tamly 500 automatically notif achar async) All;Achar:Au" +
            "toSync(notif all services async and sync achar)")]
        public string SyncServicesDescription {
            get {
                return ((string)(this["SyncServicesDescription"]));
            }
            set {
                this["SyncServicesDescription"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("60")]
        public int SyncChargedTriedNDaysBefore {
            get {
                return ((int)(this["SyncChargedTriedNDaysBefore"]));
            }
            set {
                this["SyncChargedTriedNDaysBefore"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(Now - regDate).Minutes>SyncFtpWaitTimeInMins (should wait for new ftp)")]
        public string SyncFtpWaitTimeInMinsDescription {
            get {
                return ((string)(this["SyncFtpWaitTimeInMinsDescription"]));
            }
            set {
                this["SyncFtpWaitTimeInMinsDescription"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(Now - max(activationDate,deactivationDate).Minutes>SyncDBWaitTimeInMins (should " +
            "wait may be we will be notif)")]
        public string SyncDBWaitTimeInMinsDescription {
            get {
                return ((string)(this["SyncDBWaitTimeInMinsDescription"]));
            }
            set {
                this["SyncDBWaitTimeInMinsDescription"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(max(activationDate,deactivationDate) - ftpDateTime).SyncFtpOldItemsInMins (ftp i" +
            "tem is really old -1 means do not apply this setting)")]
        public string SyncFtpOldItemsInMinsDescription {
            get {
                return ((string)(this["SyncFtpOldItemsInMinsDescription"]));
            }
            set {
                this["SyncFtpOldItemsInMinsDescription"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("if we have any try in ndays before consider this user active")]
        public string SyncChargedTriedNDaysBeforeDescription {
            get {
                return ((string)(this["SyncChargedTriedNDaysBeforeDescription"]));
            }
            set {
                this["SyncChargedTriedNDaysBeforeDescription"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15")]
        public int MobinOneSyncNDaysBefore {
            get {
                return ((int)(this["MobinOneSyncNDaysBefore"]));
            }
            set {
                this["MobinOneSyncNDaysBefore"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("E:\\ImiFtps\\MobinOne")]
        public string LocalPathMobinOne {
            get {
                return ((string)(this["LocalPathMobinOne"]));
            }
            set {
                this["LocalPathMobinOne"] = value;
            }
        }
    }
}
