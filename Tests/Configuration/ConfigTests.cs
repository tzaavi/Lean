﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.Configuration
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void SetRespectsEnvironment()
        {
            bool betaMode = Config.GetBool("beta-mode");
            var env = Config.Get("environment");
            Config.Set(env + ".beta-mode", betaMode ? "false" : "true");


            bool betaMode2 = Config.GetBool("beta-mode");
            Assert.AreNotEqual(betaMode, betaMode2);
        }

        [Test]
        public void GetValueHandlesDateTime()
        {
            GetValueHandles(new DateTime(2015, 1, 2, 3, 4, 5));
        }

        [Test]
        public void GetValueHandlesTimeSpan()
        {
            GetValueHandles(new TimeSpan(1, 2, 3, 4, 5));
        }

        private void GetValueHandles<T>(T value)
        {
            var configValue = value.ToString();
            Config.Set("temp-value", configValue);
            var actual = Config.GetValue<T>("temp-value");
            Assert.AreEqual(value, actual);
        }
    }
}
