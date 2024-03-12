/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2023 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

using HpToolsLauncher.Utils;
using Microsoft.Win32;
using QTObjectModelLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Action = QTObjectModelLib.Action;

namespace HpToolsLauncher
{
    public class MBTRunner(string parentFolder, string repoFolder, IEnumerable<MBTTest> tests) : RunnerBase, IDisposable
    {
        private readonly object _lockObject = new();
        private readonly string _parentFolder = parentFolder;//folder in which we will create new tests
        private readonly string _repoFolder = repoFolder;
        private readonly IEnumerable<MBTTest> _mbtTests = tests;
        private readonly IList<KeyValuePair<string, object>> _rnrMobilePaths = [];
        private readonly IList<KeyValuePair<string, object>> _rnrWebPaths = [];

        private const string MOBILE_STARTUP_SETTINGS_REG_KEY = @"Software\Mercury Interactive\QuickTest Professional\MicTest\AddIn Manager\Mobile\Startup Settings";
        private const string WEB_STARTUP_SETTINGS_REG_KEY = @"Software\Mercury Interactive\QuickTest Professional\MicTest\AddIn Manager\Web\Startup Settings";
        private const string MOBILE_STARTUP_SETTINGS = @"AddIn Manager\Mobile\Startup Settings";
        private const string WEB_STARTUP_SETTINGS = @"AddIn Manager\Web\Startup Settings";
        private const string HISTORY = "History";
        private const string _DEFAULT = "_default";

        public override TestSuiteRunResults Run()
        {
            var type = Type.GetTypeFromProgID("Quicktest.Application");

            lock (_lockObject)
            {
                Application qtpApp = Activator.CreateInstance(type) as Application;
                try
                {
                    if (Directory.Exists(_parentFolder))
                    {
                        Directory.Delete(_parentFolder, true);
                    }
                    ConsoleWriter.WriteLine("Using parent folder : " + _parentFolder);
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine("Failed to delete parent folder : " + e.Message);
                }

                Directory.CreateDirectory(_parentFolder);
                DirectoryInfo parentDir = new(_parentFolder);

                try
                {
                    if (qtpApp.Launched)
                    {
                        qtpApp.Quit();
                    }
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine("Failed to close qtpApp: " + e.Message);
                }

                //START Test creation
                foreach (var mbtTest in _mbtTests)
                {
                    ResetKvpListsValues();
                    DateTime dtStartOfTest = DateTime.Now;
                    ConsoleWriter.WriteLine("Creation of " + mbtTest.Name + " *****************************");
                    string[] addins = LoadNeededAddins(qtpApp, mbtTest.UnderlyingTests);
                    ConsoleWriter.WriteLine(string.Format("LoadNeededAddins took {0:0.0} secs", (DateTime.Now - dtStartOfTest).TotalSeconds));
                    try
                    {
                        string firstUnderlyingTest = mbtTest.UnderlyingTests.FirstOrDefault(t => !t.IsNullOrEmpty());
                        DateTime dtStartOfStep;
                        dtStartOfStep = DateTime.Now;
                        GetMobileAndWebSettings(qtpApp, firstUnderlyingTest, out bool hasMobileSettings, out bool hasWebSettings);
                        ConsoleWriter.WriteLine(string.Format("Get Mobile and/or Web Settings took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                        dtStartOfStep = DateTime.Now;
                        qtpApp.New();
                        ConsoleWriter.WriteLine(string.Format("qtpApp.New took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                        if (hasMobileSettings)
                        {
                            dtStartOfStep = DateTime.Now;
                            SetTestOptionsValWithPath(qtpApp, _rnrMobilePaths, MOBILE_STARTUP_SETTINGS);
                            ConsoleWriter.WriteLine(string.Format("Set Mobile Settings took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                        }
                        if (hasWebSettings)
                        {
                            dtStartOfStep = DateTime.Now;
                            SetTestOptionsValWithPath(qtpApp, _rnrWebPaths, WEB_STARTUP_SETTINGS);
                            ConsoleWriter.WriteLine(string.Format("Set Web Settings took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                        }
                        Test test = qtpApp.Test;
                        if (addins?.Length > 0)
                        {
                            dtStartOfStep = DateTime.Now;
                            test.SetAssociatedAddins(addins, out object err);
                            ConsoleWriter.WriteLine(string.Format("test.SetAssociatedAddins took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                            if (!((string)err).IsNullOrEmpty())
                            {
                                ConsoleWriter.WriteErrLine("Failed to SetAssociatedAddins: " + err);
                            }
                        }

                        Action action1 = test.Actions[1];
                        action1.Description = "unitIds=" + string.Join(",", mbtTest.UnitIds);

                        //https://myskillpoint.com/how-to-use-loadandrunaction-in-uft/#LoadAndRunAction_Having_Input-Output_Parameters
                        //LoadAndRunAction "E:\UFT_WorkSpace\TestScripts\SampleTest","Action1",0,"inputParam1","inputParam2",outParameterVal
                        //string actionContent = "LoadAndRunAction \"c:\\Temp\\GUITest2\\\",\"Action1\"";
                        string actionContent = File.Exists(mbtTest.Script) ? File.ReadAllText(mbtTest.Script) : mbtTest.Script;
                        action1.ValidateScript(actionContent);
                        action1.SetScript(actionContent);

                        DirectoryInfo fullDir = parentDir;
                        if (!mbtTest.PackageName.IsNullOrEmpty())
                        {
                            fullDir = fullDir.CreateSubdirectory(mbtTest.PackageName);
                        }

                        //Expects to receive params in CSV format, encoded base64
                        if (!mbtTest.DatableParams.IsNullOrEmpty())
                        {
                            string tempCsvFileName = Path.Combine(_parentFolder, "temp.csv");
                            if (File.Exists(tempCsvFileName))
                            {
                                File.Delete(tempCsvFileName);
                            }

                            byte[] data = Convert.FromBase64String(mbtTest.DatableParams);
                            string decodedParams = Encoding.UTF8.GetString(data);

                            File.WriteAllText(tempCsvFileName, decodedParams);
                            test.DataTable.Import(tempCsvFileName);
                            File.Delete(tempCsvFileName);
                        }

                        string fullPath = fullDir.CreateSubdirectory(mbtTest.Name).FullName;
                        test.SaveAs(fullPath);
                        qtpApp.Test.Close();
                        ConsoleWriter.WriteLine(string.Format("MBT test was created in {0} in {1:0.0} secs", fullPath, (DateTime.Now - dtStartOfTest).TotalSeconds));
                    }
                    catch (Exception e)
                    {
                        ConsoleWriter.WriteErrLine("Failed in MBTRunner : " + e.Message);
                        ConsoleWriter.WriteLine(e.StackTrace);
                    }
                }
                if (qtpApp.Launched)
                {
                    qtpApp.Quit();
                    Console.WriteLine($"Closed UFT One.");
                }
            }

            return null;
        }

        private void ResetKvpListsValues()
        {
            resetPathListValues(_rnrMobilePaths);
            resetPathListValues(_rnrWebPaths);

            static void resetPathListValues(IList<KeyValuePair<string, object>> list)
            {
                if (list.Any())
                {
                    for (int x = 0; x < list.Count; x++)
                    {
                        KeyValuePair<string, object> kvp = list[x];
                        if (kvp.Value != null)
                            list[x] = new(kvp.Key, null);
                    }
                }
            }
        }

        private string GetResourceFileNameAndAddToUftFoldersIfRequired(Application qtpApplication, string filePath)
        {
            //file path might be full or just file name;
            string location = qtpApplication.Folders.Locate(filePath);
            if (!location.IsNullOrEmpty())
            {
                ConsoleWriter.WriteLine(string.Format("Adding resources : {0} - done", filePath));
            }
            else
            {
                ConsoleWriter.WriteLine(string.Format("Adding resources : {0} - failed to find file in repository. Please check correctness of resource location.", filePath));
            }

            return filePath;
        }

        private void GetMobileAndWebSettings(Application app, string testPath, out bool hasMobileSettings, out bool hasWebSettings)
        {
            Console.WriteLine($"Opening the firstUnderlyingTest: [{testPath}]");
            app.Open(testPath, true);
            hasMobileSettings = hasWebSettings = false;
            Launchers launchers = app.Test.Settings.Launchers;
            Console.WriteLine($"launchers.Count = {launchers.Count}");
            foreach (object lnc in app.Test.Settings.Launchers)
            {
                if (lnc is MobileLauncher)
                {
                    Console.WriteLine($"GET Mobile Startup Settings");
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(MOBILE_STARTUP_SETTINGS_REG_KEY, false);
                    if (!_rnrMobilePaths.Any())
                        GetSubKeyNamesRecursively(key, _rnrMobilePaths);
                    GetTestOptionsValWithPath(app, _rnrMobilePaths, MOBILE_STARTUP_SETTINGS);
                    hasMobileSettings = true;
                }
                else if (lnc is WebLauncher)
                {
                    Console.WriteLine($"GET Web Startup Settings");
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(WEB_STARTUP_SETTINGS_REG_KEY, false);
                    if (!_rnrWebPaths.Any())
                        GetSubKeyNamesRecursively(key, _rnrWebPaths);
                    GetTestOptionsValWithPath(app, _rnrWebPaths, WEB_STARTUP_SETTINGS);
                    hasWebSettings = true;
                }
            }
            app.Test.Close();
            Console.WriteLine($"Closed the firstUnderlyingTest: [{testPath}]");
        }
        void SetTestOptionsValWithPath(Application app, IList<KeyValuePair<string, object>> paths, string startupSettings)
        {
            for (int x = 0; x < paths.Count; x++)
            {
                KeyValuePair<string, object> kvp = paths[x];
                app.TDPierToTulip.SetTestOptionsValWithPath($@"{startupSettings}\{kvp.Key}", _DEFAULT, kvp.Value);
            }
        }

        private void GetTestOptionsValWithPath(Application app, IList<KeyValuePair<string, object>> paths, string startupSettings)
        {
            for (int x = 0; x < paths.Count; x++)
            {
                KeyValuePair<string, object> kvp = paths[x];
                app.TDPierToTulip.GetTestOptionsValWithPath($@"{startupSettings}\{kvp.Key}", _DEFAULT, out object obj);
                if (obj != null)
                {
                    paths[x] = new(kvp.Key, obj);
                }
            }
        }

        private string[] LoadNeededAddins(Application _qtpApplication, IEnumerable<string> fileNames)
        {
            string[] addins = null;
            try
            {
                HashSet<string> addinsSet = [];
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        DateTime start1 = DateTime.Now;
                        object[] testAddinsObj = (object[])_qtpApplication.GetAssociatedAddinsForTest(fileName);
                        ConsoleWriter.WriteLine(string.Format("GetAssociatedAddinsForTest took {0:0.0} secs", DateTime.Now.Subtract(start1).TotalSeconds));
                        IEnumerable<string> tempTestAddins = testAddinsObj.Cast<string>();

                        foreach (string addin in tempTestAddins)
                        {
                            addinsSet.Add(addin);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleWriter.WriteErrLine("Failed to LoadNeededAddins for : " + fileName + ", " + ex.Message);
                    }
                }

                addins = new string[addinsSet.Count];
                addinsSet.CopyTo(addins);
                ConsoleWriter.WriteLine("Loading Addins : " + string.Join(",", addins));
                DateTime start2 = DateTime.Now;
                _qtpApplication.SetActiveAddins(addins, out object err);
                ConsoleWriter.WriteLine(string.Format("SetActiveAddins took {0:0.0} secs", DateTime.Now.Subtract(start2).TotalSeconds));
                if (!((string)err).IsNullOrEmpty())
                {
                    ConsoleWriter.WriteErrLine("Failed to SetActiveAddins : " + err);
                }
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteErrLine("Failed to LoadNeededAddins : " + ex.Message);
                // Try anyway to run the test
            }
            return addins;
        }

        private void GetSubKeyNamesRecursively(RegistryKey regKey, IList<KeyValuePair<string, object>> list, string suffix = "")
        {
            if (regKey.SubKeyCount > 0)
            {
                string[] subKeys = regKey.GetSubKeyNames();
                foreach (string subKey in subKeys)
                {
                    string s = suffix == string.Empty ? subKey : $@"{suffix}\{subKey}";
                    list.Add(new(s, null));
                    if (subKey != HISTORY)
                    {
                        RegistryKey regSubKey = regKey.OpenSubKey(subKey, false);
                        GetSubKeyNamesRecursively(regSubKey, list, s);
                    }
                }
            }
        }
    }

    public class RecoveryScenario
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }

        public static RecoveryScenario ParseFromString(string content)
        {
            RecoveryScenario rs = new();
            string[] parts = content.Split(',');//expected 3 parts separated by , : location,name,position(default is -1)
            if (parts.Length < 2)
            {
                ConsoleWriter.WriteErrLine("Failed to parse recovery scenario (need at least 2 parts, separated with ,): " + content);
                return null;
            }
            rs.FileName = parts[0];
            rs.Name = parts[1];
            if (parts.Length >= 3)
            {
                try
                {
                    rs.Position = int.Parse(parts[2]);
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine("Failed to parse position of recovery scenario : " + content + " : " + e.Message);
                    rs.Position = -1;
                }
            }
            else
            {
                rs.Position = -1;
            }

            return rs;
        }
    }

    public class MBTTest
    {
        public string Name { get; set; }
        public string Script { get; set; }
        public string UnitIds { get; set; }
        public List<string> UnderlyingTests { get; set; }
        public string PackageName { get; set; }
        public string DatableParams { get; set; }
    }
}
