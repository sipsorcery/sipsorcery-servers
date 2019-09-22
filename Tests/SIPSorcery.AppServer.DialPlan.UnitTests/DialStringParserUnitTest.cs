using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIPSorcery.Persistence;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace SIPSorcery.AppServer.DialPlan.UnitTests
{
    [TestClass]
    public class DialStringParserUnitTest
    {
        [TestMethod]
        [Ignore] // Broken after NEtStandard migration.
        public void SingleProviderLegUnitTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:1234@localhost"));
            SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader("<sip:joe@localhost>"), SIPToHeader.ParseToHeader("<sip:jane@localhost>"), 23, CallProperties.CreateNewCallId());
            SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 5060, CallProperties.CreateBranchId());
            inviteHeader.Vias.PushViaHeader(viaHeader);
            inviteRequest.Header = inviteHeader;

            List<SIPProvider> providers = new List<SIPProvider>();
            SIPProvider provider = new SIPProvider(ProviderTypes.SIP, "test", "blueface", "test", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false, null, null, null);
            providers.Add(provider);

            DialStringParser dialStringParser = new DialStringParser(null, "test", null, providers, delegate { return null; }, delegate { return null; }, (host, wildcard) => { return null; }, null, "test");
            Queue<List<SIPCallDescriptor>> callQueue = dialStringParser.ParseDialString(DialPlanContextsEnum.Script, inviteRequest, "blueface", null, null, null, null, null, null, null, null, CustomerServiceLevels.Free);

            Assert.IsNotNull(callQueue, "The call list should have contained a call.");
            Assert.IsTrue(callQueue.Count == 1, "The call queue list should have contained one leg.");

            List<SIPCallDescriptor> firstLeg = callQueue.Dequeue();

            Assert.IsNotNull(firstLeg, "The first call leg should exist.");
            Assert.IsTrue(firstLeg.Count == 1, "The first call leg should have had one switch call.");
            Assert.IsTrue(firstLeg[0].Username == "test", "The username for the first call leg was not correct.");
            Assert.IsTrue(firstLeg[0].Uri.ToString() == "sip:1234@sip.blueface.ie", "The destination URI for the first call leg was not correct.");

            Console.WriteLine("---------------------------------");
        }

        //[TestMethod]
        //public void SingleProviderWithDstLegUnitTest()
        //{
        //    Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

        //    SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:1234@localhost"));
        //    SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader("<sip:joe@localhost>"), SIPToHeader.ParseToHeader("<sip:jane@localhost>"), 23, CallProperties.CreateNewCallId());
        //    SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 5060, CallProperties.CreateBranchId());
        //    inviteHeader.Vias.PushViaHeader(viaHeader);
        //    inviteRequest.Header = inviteHeader;

        //    List<SIPProvider> providers = new List<SIPProvider>();
        //    SIPProvider provider = new SIPProvider("test", "blueface", "test", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false);
        //    providers.Add(provider);

        //    DialStringParser dialStringParser = new DialStringParser(null, "test", null, providers, delegate { return null; }, null, (host, wildcard) => { return null; }, null);
        //    Queue<List<SIPCallDescriptor>> callQueue = dialStringParser.ParseDialString(DialPlanContextsEnum.Script, inviteRequest, "303@blueface", null, null, null, null, null);

        //    Assert.IsNotNull(callQueue, "The call list should have contained a call.");
        //    Assert.IsTrue(callQueue.Count == 1, "The call queue list should have contained one leg.");

        //    List<SIPCallDescriptor> firstLeg = callQueue.Dequeue();

        //    Assert.IsNotNull(firstLeg, "The first call leg should exist.");
        //    Assert.IsTrue(firstLeg.Count == 1, "The first call leg should have had one switch call.");
        //    Assert.IsTrue(firstLeg[0].Username == "test", "The username for the first call leg was not correct.");
        //    Assert.IsTrue(firstLeg[0].Uri.ToString() == "sip:303@sip.blueface.ie", "The destination URI for the first call leg was not correct.");

        //    Console.WriteLine("---------------------------------");
        //}

        //[TestMethod]
        //public void NoMatchingProviderUnitTest()
        //{
        //    Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

        //    SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:1234@localhost"));
        //    SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader("<sip:joe@localhost>"), SIPToHeader.ParseToHeader("<sip:jane@localhost>"), 23, CallProperties.CreateNewCallId());
        //    SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 5060, CallProperties.CreateBranchId());
        //    inviteHeader.Vias.PushViaHeader(viaHeader);
        //    inviteRequest.Header = inviteHeader;

        //    List<SIPProvider> providers = new List<SIPProvider>();
        //    SIPProvider provider = new SIPProvider("test", "blueface", "test", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false);
        //    providers.Add(provider);

        //    DialStringParser dialStringParser = new DialStringParser(null, "test", null, providers, delegate { return null; }, null, (host, wildcard) => { return null; }, null);
        //    Queue<List<SIPCallDescriptor>> callQueue = dialStringParser.ParseDialString(DialPlanContextsEnum.Script, inviteRequest, "303@noprovider", null, null, null, null, null);

        //    Assert.IsNotNull(callQueue, "The call list should be returned.");
        //    Assert.IsTrue(callQueue.Count == 1, "The call queue list should not have contained one leg.");
        //    List<SIPCallDescriptor> firstLeg = callQueue.Dequeue();

        //    Assert.IsNotNull(firstLeg, "The first call leg should exist.");
        //    Assert.IsTrue(firstLeg.Count == 1, "The first call leg should have had one switch call.");
        //    Assert.IsTrue(firstLeg[0].Username == DialStringParser.m_anonymousUsername, "The username for the first call leg was not correct.");
        //    Assert.IsTrue(firstLeg[0].Uri.ToString() == "sip:303@noprovider", "The destination URI for the first call leg was not correct.");

        //    Console.WriteLine("---------------------------------");
        //}

        //[TestMethod]
        //public void LookupSIPAccountUnitTest()
        //{
        //    Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

        //    DialStringParser dialStringParser = new DialStringParser(null, "test", null, null, (where) => { return null; }, (where, offset, count, orderby) => { return null; }, (host, wildcard) => { return host; }, null);
        //    Queue<List<SIPCallDescriptor>> callList = dialStringParser.ParseDialString(DialPlanContextsEnum.Script, null, "aaron@local", null, null, null, null, null);

        //    Assert.IsTrue(callList.Dequeue().Count == 0, "No local contacts should have been returned.");

        //    Console.WriteLine("---------------------------------");
        //}

        //[TestMethod]
        //public void MultipleForwardsUnitTest()
        //{
        //    Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

        //    SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:1234@localhost"));
        //    SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader("<sip:joe@localhost>"), SIPToHeader.ParseToHeader("<sip:jane@localhost>"), 23, CallProperties.CreateNewCallId());
        //    SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 5060, CallProperties.CreateBranchId());
        //    inviteHeader.Vias.PushViaHeader(viaHeader);
        //    inviteRequest.Header = inviteHeader;

        //    List<SIPProvider> providers = new List<SIPProvider>();
        //    SIPProvider provider = new SIPProvider("test", "provider1", "user", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false);
        //    SIPProvider provider2 = new SIPProvider("test", "provider2", "user", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false);
        //    providers.Add(provider);
        //    providers.Add(provider2);

        //    DialStringParser dialStringParser = new DialStringParser(null, "test", null, providers, (where) => { return null; }, (where, offset, count, orderby) => { return null; }, (host, wildcard) => { return null; }, null);
        //    Queue<List<SIPCallDescriptor>> callQueue = dialStringParser.ParseDialString(DialPlanContextsEnum.Script, inviteRequest, "provider1&provider2", null, null, null, null, null);

        //    Assert.IsNotNull(callQueue, "The call list should have contained a call.");
        //    Assert.IsTrue(callQueue.Count == 1, "The call queue list should have contained one leg.");

        //    List<SIPCallDescriptor> firstLeg = callQueue.Dequeue();

        //    Assert.IsNotNull(firstLeg, "The first call leg should exist.");
        //    Assert.IsTrue(firstLeg.Count == 2, "The first call leg should have had two switch calls.");

        //    Console.WriteLine("---------------------------------");
        //}

        //[TestMethod]
        //public void MultipleForwardsWithLocalUnitTest()
        //{
        //    Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

        //    SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:1234@localhost"));
        //    SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader("<sip:joe@localhost>"), SIPToHeader.ParseToHeader("<sip:jane@localhost>"), 23, CallProperties.CreateNewCallId());
        //    SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 5060, CallProperties.CreateBranchId());
        //    inviteHeader.Vias.PushViaHeader(viaHeader);
        //    inviteRequest.Header = inviteHeader;

        //    List<SIPProvider> providers = new List<SIPProvider>();
        //    SIPProvider provider = new SIPProvider("test", "provider1", "user", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false);
        //    SIPProvider provider2 = new SIPProvider("test", "provider2", "user", "password", SIPURI.ParseSIPURIRelaxed("sip.blueface.ie"), null, null, null, null, 3600, null, null, null, false, false);
        //    providers.Add(provider);
        //    providers.Add(provider2);

        //    DialStringParser dialStringParser = new DialStringParser(null, "test", null, providers, (where) => { return null; }, (where, offset, count, orderby) => { return null; }, (host, wildcard) => { return null; }, null);
        //    Queue<List<SIPCallDescriptor>> callQueue = dialStringParser.ParseDialString(DialPlanContextsEnum.Script, inviteRequest, "local&1234@provider2", null, null, null, null, null);

        //    Assert.IsNotNull(callQueue, "The call list should have contained a call.");
        //    Assert.IsTrue(callQueue.Count == 1, "The call queue list should have contained one leg.");

        //    List<SIPCallDescriptor> firstLeg = callQueue.Dequeue();

        //    Assert.IsNotNull(firstLeg, "The first call leg should exist.");
        //    Assert.IsTrue(firstLeg.Count == 2, "The first call leg should have had two switch calls.");

        //    Console.WriteLine("First destination uri=" + firstLeg[0].Uri.ToString());
        //    Console.WriteLine("Second destination uri=" + firstLeg[1].Uri.ToString());

        //    Console.WriteLine("---------------------------------");
        //}

        [TestMethod]
        public void SubstitueDstVarTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            request.Header = new SIPHeader();
            string substitutedString = DialStringParser.SubstituteRequestVars(request, "${dst}123");

            Console.Write("Substituted string=" + substitutedString + ".");

            Assert.IsTrue(substitutedString == "380123", "The destination was not substituted correctly.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SubstitueEmptyFromNameTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            request.Header = new SIPHeader();
            string substitutedString = DialStringParser.SubstituteRequestVars(request, "${fromname} <sip:user@provider>");

            Console.Write("Substituted string=" + substitutedString + ".");

            Assert.IsTrue(substitutedString == "<sip:user@provider>", "The from header was not substituted correctly.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SubstitueDoubleDstVarTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            request.Header = new SIPHeader();
            string substitutedString = DialStringParser.SubstituteRequestVars(request, "${dst}123${dst}");

            Console.Write("Substituted string=" + substitutedString + ".");

            Assert.IsTrue(substitutedString == "380123380", "The destination was not substituted correctly.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SubstitueDoubleDstSubStrVarTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            request.Header = new SIPHeader();
            string substitutedString = DialStringParser.SubstituteRequestVars(request, "${dst:1}123${dst:2}");

            Console.Write("Substituted string=" + substitutedString + ".");

            Assert.IsTrue(substitutedString == "801230", "The destination was not substituted correctly.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SubstitueMixedDstSubStrVarTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            request.Header = new SIPHeader();
            string substitutedString = DialStringParser.SubstituteRequestVars(request, "${dst:1}123${dst}000");

            Console.Write("Substituted string=" + substitutedString + ".");

            Assert.IsTrue(substitutedString == "80123380000", "The destination was not substituted correctly.");

            Console.WriteLine("---------------------------------");
        }
    }
}
