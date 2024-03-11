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
using Mercury.TD.Client.Ota.QC9;
using QTObjectModelLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Action = QTObjectModelLib.Action;
using Test = QTObjectModelLib.Test;

namespace HpToolsLauncher
{
    public class MBTRunner(string parentFolder, string repoFolder, IEnumerable<MBTTest> tests) : RunnerBase, IDisposable
    {
        private readonly object _lockObject = new();
        private readonly string parentFolder = parentFolder;//folder in which we will create new tests
        private readonly string repoFolder = repoFolder;
        private readonly IEnumerable<MBTTest> mbtTests = tests;

        private const string MOBILE_JOB_SETTINGS = @"AddIn Manager\Mobile\Startup Settings\JOB_SETTINGS";
        private const string MOBILE_APP_NAME = @"AddIn Manager\Mobile\Startup Settings\ApplicationName";
        private const string WEB_MOBILE_JOB_SETTINGS = @"AddIn Manager\Web\Startup Settings\Web_Mobile\Mobile\JOB_SETTINGS";
        private const string WEB_MOBILE_SELECTED_APP = @"Addin Manager\Web\Startup Settings\Web_Mobile\Mobile\Selected_Application";
        private const string WEB_MOBILE_SELECTED_DEVICE = @"Addin Manager\Web\Startup Settings\Web_Mobile\Mobile\Selected_Device";
        private const string WEB_MOBILE_SELECTED_VITALS = @"Addin Manager\Web\Startup Settings\Web_Mobile\Mobile\Selected_Vitals";
        private const string _DEFAULT = "_default";
        private const string WEB = "Web";
        private const string MOBILE = "Mobile";
        private const string DIGITAL_LAB = "DigitalLab";

        private class WebLauncherEx(bool active, /*string env,*/ string address, string browser, string browserParamName)
        {
            public bool Active => active;
            public string Address => address;
            public string Browser => browser;
            //public string Env => env;
            public string BrowserParamName => browserParamName;
        }

        private class MobileSettings(string job, string appName, string jobWeb, string selectedApp, string selectedDevice, string selectedVitals, WebLauncher webLauncher)
        {
            private readonly WebLauncherEx webLauncherEx = webLauncher != null ?
                            new(webLauncher.Active, webLauncher.Address, webLauncher.Browser, webLauncher.BrowserParameterName) :
                            null;

            public string Job => job;
            public string JobWeb => jobWeb;
            public string AppName => appName;
            public string SelectedApp => selectedApp;
            public string SelectedDevice => selectedDevice;
            public string SelectedVitals => selectedVitals;
            public WebLauncherEx WebLauncherEx => webLauncherEx;
        }

        public override TestSuiteRunResults Run()
        {
            var type = Type.GetTypeFromProgID("Quicktest.Application");

            lock (_lockObject)
            {
                Application qtpApp = Activator.CreateInstance(type) as Application;
                try
                {
                    if (Directory.Exists(parentFolder))
                    {
                        Directory.Delete(parentFolder, true);
                    }
                    ConsoleWriter.WriteLine("Using parent folder : " + parentFolder);
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine("Failed to delete parent folder : " + e.Message);
                }

                Directory.CreateDirectory(parentFolder);
                DirectoryInfo parentDir = new(parentFolder);

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
                foreach (var mbtTest in mbtTests)
                {
                    DateTime dtStartOfTest = DateTime.Now;
                    ConsoleWriter.WriteLine("Creation of " + mbtTest.Name + " *****************************");
                    string[] addins = LoadNeededAddins(qtpApp, mbtTest.UnderlyingTests);
                    ConsoleWriter.WriteLine(string.Format("LoadNeededAddins took {0:0.0} secs", (DateTime.Now - dtStartOfTest).TotalSeconds));
                    try
                    {
                        string firstUnderlyingTest = mbtTest.UnderlyingTests.FirstOrDefault(t => !t.IsNullOrEmpty());
                        MobileSettings settings = null;
                        DateTime dtStartOfStep;
                        if (!firstUnderlyingTest.IsNullOrEmpty())
                        {
                            dtStartOfStep = DateTime.Now;
                            settings = GetMobileSettings(qtpApp, firstUnderlyingTest);
                            ConsoleWriter.WriteLine(string.Format("GetJobSettings took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                            Console.WriteLine($"Job Settings: {settings.Job}");
                            Console.WriteLine($"App Name: {settings.AppName}");
                            Console.WriteLine($"Web JobSettings: {settings.JobWeb}");
                            Console.WriteLine($"Selected App: {settings.SelectedApp}");
                            Console.WriteLine($"Selected Vitals: {settings.SelectedVitals}");
                        }
                        dtStartOfStep = DateTime.Now;
                        qtpApp.New();
                        ConsoleWriter.WriteLine(string.Format("qtpApp.New took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
                        if (settings != null)
                        {
                            dtStartOfStep = DateTime.Now;
                            SetTestOptionsValWithPath(MOBILE_JOB_SETTINGS, settings.Job);
                            SetTestOptionsValWithPath(MOBILE_APP_NAME, settings.AppName);
                            SetTestOptionsValWithPath(WEB_MOBILE_JOB_SETTINGS, settings.JobWeb);
                            SetTestOptionsValWithPath(WEB_MOBILE_SELECTED_APP, settings.SelectedApp);
                            SetTestOptionsValWithPath(WEB_MOBILE_SELECTED_DEVICE, settings.SelectedDevice);
                            SetTestOptionsValWithPath(WEB_MOBILE_SELECTED_VITALS, settings.SelectedVitals);
                            ConsoleWriter.WriteLine(string.Format("Set Test Options took {0:0.0} secs", (DateTime.Now - dtStartOfStep).TotalSeconds));
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
                            string tempCsvFileName = Path.Combine(parentFolder, "temp.csv");
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
                        ConsoleWriter.WriteLine(string.Format("MBT test was created in {0} in {1:0.0} secs", fullPath, (DateTime.Now - dtStartOfTest).TotalSeconds));
                        qtpApp.Quit();
                        qtpApp = Activator.CreateInstance(type) as Application;
                        Console.WriteLine($@"Activating the labs ...");
                        qtpApp.Open(fullPath, false, true);
                        Launchers launchers = qtpApp.Test.Settings.Launchers;
                        Console.WriteLine($"launchers.Count = {launchers.Count}");
                        foreach (var lan in launchers)
                        {
                            if (lan is WebLauncher webLnc)
                            {
                                WebLauncherEx webLncEx = settings.WebLauncherEx;
                                //webLnc.SetLab("MobileBrowser");
                                webLnc.Active = webLncEx.Active;
                                //webLnc.Env = webLncEx.Env;
                                webLnc.Address = webLncEx.Address;
                                webLnc.Browser = webLncEx.Browser;
                                webLnc.BrowserParameterName = webLncEx.BrowserParamName;
                                Console.WriteLine($"WebLauncher is loaded and Active = {webLnc.Active}");
                            }
                            else if (lan is MobileLauncher mobileLnc)
                            {
                                mobileLnc.Lab = DIGITAL_LAB;
                                Console.WriteLine($"MobileLauncher is loaded and Lab = {mobileLnc.Lab}");
                            }
                        }
                        qtpApp.Test.Save();
                        qtpApp.Test.Close();
                        Console.WriteLine($"Saved and closed [{fullPath}].");
                    }
                    catch (Exception e)
                    {
                        ConsoleWriter.WriteErrLine("Failed in MBTRunner : " + e.Message);
                        ConsoleWriter.WriteLine(e.StackTrace);
                    }
                }
                if (qtpApp.Launched)
                {
                    Console.WriteLine($"Trying to close UFT One...");
                    qtpApp.Quit();
                    Console.WriteLine($"Closed UFT One.");
                }

                void SetTestOptionsValWithPath(string path, string value)
                {
                    if (!value.IsNullOrEmpty())
                    {
                        qtpApp.TDPierToTulip.SetTestOptionsValWithPath(path, _DEFAULT, value);
                    }
                }
            }

            return null;
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

        private MobileSettings GetMobileSettings(Application qtApp, string testPath)
        {
            qtApp.Open(testPath, true);
            qtApp.TDPierToTulip.GetTestOptionsValWithPath(MOBILE_JOB_SETTINGS, _DEFAULT, out object objJobSettings);
            qtApp.TDPierToTulip.GetTestOptionsValWithPath(MOBILE_APP_NAME, _DEFAULT, out object objAppName);
            qtApp.TDPierToTulip.GetTestOptionsValWithPath(WEB_MOBILE_JOB_SETTINGS, _DEFAULT, out object objJobWebSettings);
            qtApp.TDPierToTulip.GetTestOptionsValWithPath(WEB_MOBILE_SELECTED_APP, _DEFAULT, out object objWebSelectedApp);
            qtApp.TDPierToTulip.GetTestOptionsValWithPath(WEB_MOBILE_SELECTED_DEVICE, _DEFAULT, out object objWebSelectedDevice);
            qtApp.TDPierToTulip.GetTestOptionsValWithPath(WEB_MOBILE_SELECTED_VITALS, _DEFAULT, out object objWebSelectedVitals);

            Launchers launchers = qtApp.Test.Settings.Launchers;
            WebLauncher webLauncher = null;
            foreach (var lan in launchers)
            {
                if (lan is WebLauncher webLnc)
                {
                    webLauncher = webLnc;
                    Console.WriteLine($"WebLauncher is loaded");
                    Console.WriteLine($"Active = {webLnc.Active}");
                    try { Console.WriteLine($"Env = {webLnc.Env}"); } catch (Exception e) { Console.WriteLine($"Env => {e.Message}"); }
                    Console.WriteLine($"Address = {webLnc.Address}");
                    Console.WriteLine($"Browser = {webLnc.Browser}");
                    Console.WriteLine($"BrowserParameterName = {webLnc.BrowserParameterName}");
                    break;
                }
            }
            MobileSettings res = new(
                        objJobSettings as string,
                        objAppName as string,
                        objJobWebSettings as string,
                        objWebSelectedApp as string,
                        objWebSelectedDevice as string,
                        objWebSelectedVitals as string,
                        webLauncher
                );
            qtApp.Test.Close();
            return res;
        }
        private string[] LoadNeededAddins(Application _qtpApplication, IEnumerable<string> fileNames)
        {
            string[] addins = null;
            try
            {
                HashSet<string> addinsSet = new HashSet<string>();
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
    }

    public class RecoveryScenario
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }

        public static RecoveryScenario ParseFromString(string content)
        {
            RecoveryScenario rs = new RecoveryScenario();
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
