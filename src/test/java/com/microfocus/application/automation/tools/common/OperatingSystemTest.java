/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2024 Open Text
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

package com.microfocus.application.automation.tools.common;

import com.microfocus.application.automation.tools.common.utils.OperatingSystem;
import org.junit.AfterClass;
import org.junit.Assert;
import org.junit.BeforeClass;
import org.junit.Test;

import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;

import static junit.framework.TestCase.assertEquals;

public class OperatingSystemTest {
    private static String os;

    public static void initializeOperatingSystemOs(final String os)
            throws NoSuchFieldException, IllegalAccessException {
        OperatingSystem.refreshOsVariablesForSlave();
    }

    private static void setAllBooleanStaticFinalFields(boolean isWindows, boolean isMac, boolean isLinux)
            throws NoSuchFieldException, IllegalAccessException {
        changeBooleanStaticFinalField(isWindows, "windows");
        changeBooleanStaticFinalField(isMac, "mac");
        changeBooleanStaticFinalField(isLinux, "linux");
    }

    private static void changeStaticFinalField(String value, String declaredField)
            throws NoSuchFieldException, IllegalAccessException {

        Field field = OperatingSystem.class.getDeclaredField(declaredField);
        field.setAccessible(true);
        Field modifiers = getModifiersField();
        modifiers.setAccessible(true);
        modifiers.setInt(field, field.getModifiers() & ~Modifier.FINAL);
        field.set(null, value.toLowerCase());
    }

    private static void changeBooleanStaticFinalField(boolean value, String declaredField)
            throws NoSuchFieldException, IllegalAccessException {

        Field field = OperatingSystem.class.getDeclaredField(declaredField);
        field.setAccessible(true);
        Field modifiers = getModifiersField();
        modifiers.setAccessible(true);
        modifiers.setInt(field, field.getModifiers() & ~Modifier.FINAL);
        field.set(null, value);
    }

    @BeforeClass
    public static void setup() {
        os = System.getProperty("os.name");
    }


    @AfterClass
    public static void tearDown() throws Exception {
        OperatingSystem.refreshOsVariablesForSlave();
    }


    @Test
    public void equalsCurrentOs_windows() throws NoSuchFieldException, IllegalAccessException {
//        initializeOperatingSystemOs("Windows 7");
        Assert.assertTrue(OperatingSystem.WINDOWS.equalsCurrentOs());
    }

    @Test
    public void equalsCurrentOs_linux() throws NoSuchFieldException, IllegalAccessException {
        String os = "Linux";
        System.setProperty("os.name",os);
        OperatingSystem.refreshOsVariablesForSlave();
//        initializeOperatingSystemOs(os);
        assertEquals("Operating system should be " + os, true, OperatingSystem.isLinux());
    }

    @Test
    public void equalsCurrentOs_mac() throws NoSuchFieldException, IllegalAccessException {
        String os = "Mac OS X";
        System.setProperty("os.name",os);
        OperatingSystem.refreshOsVariablesForSlave();
//        initializeOperatingSystemOs(os);
        assertEquals("Operating system should be " + os, true, OperatingSystem.isMac());
    }

    @Test
    public void equalsCurrentOs_invalidOsReturnsFalse()
            throws NoSuchFieldException, IllegalAccessException{
        String os = "Invalid OS";
        System.setProperty("os.name",os);
        OperatingSystem.refreshOsVariablesForSlave();
//        initializeOperatingSystemOs("Invalid OS");
        assertEquals("Operating system should be " + os, false, OperatingSystem.isWindows());
    }


    private static Field getModifiersField() throws NoSuchFieldException
    {
        try {
            return Field.class.getDeclaredField("modifiers");
        }
        catch (NoSuchFieldException e) {
            try {
                Method getDeclaredFields0 = Class.class.getDeclaredMethod("getDeclaredFields0", boolean.class);
                getDeclaredFields0.setAccessible(true);
                Field[] fields = (Field[]) getDeclaredFields0.invoke(Field.class, false);
                for (Field field : fields) {
                    if ("modifiers".equals(field.getName())) {
                        return field;
                    }
                }
            }
            catch (ReflectiveOperationException ex) {
                e.addSuppressed(ex);
            }
            throw e;
        }
    }
}