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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.IO;
using System.Text;

namespace HpToolsLauncher.Utils
{
    public static class Encrypter
    {
        private static readonly byte[] _secretKey;
        private static readonly byte[] _initVector;

        private const string AES_256_SECRET_KEY = "AES_256_SECRET_KEY";
        private const string AES_256_SECRET_INIT_VECTOR = "AES_256_SECRET_INIT_VECTOR";

        private const string ALGORITHM = "AES";
        private const string CIPHER = "AES/GCM/NoPadding";

        private static readonly IBufferedCipher _decryptor;

        static Encrypter()
        {
#if DEBUG
            return;
#endif
            string key = Environment.GetEnvironmentVariable(AES_256_SECRET_KEY);
            string iv = Environment.GetEnvironmentVariable(AES_256_SECRET_INIT_VECTOR);

            if (!key.IsNullOrEmpty() && !iv.IsNullOrEmpty())
            {
                _secretKey = Encoding.UTF8.GetBytes(key); // 32 bytes
                _initVector = Encoding.UTF8.GetBytes(iv); // 16 bytes
                KeyParameter keySpec = ParameterUtilities.CreateKeyParameter(ALGORITHM, _secretKey);
                ParametersWithIV ivSpec = new(keySpec, _initVector);
                _decryptor = CipherUtilities.GetCipher(CIPHER);
                _decryptor.Init(false, ivSpec);
            }
        }

        public static string Decrypt(string textToDecrypt)
        {
            if (textToDecrypt.IsNullOrWhiteSpace())
                return string.Empty;
#if DEBUG
            return textToDecrypt;
#endif
            byte[] bytesToDecrypt = Convert.FromBase64String(textToDecrypt);
            byte[] plaintextBytes = _decryptor.DoFinal(bytesToDecrypt);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
