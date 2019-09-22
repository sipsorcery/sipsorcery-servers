using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIPSorcery.Persistence;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace SIPSorcery.AppServer.DialPlan.UnitTests
{
    [TestClass]
    public class DialPlanEngineUnitTest
    {
        private class MockSIPDNSManager
        {
            public static SIPDNSLookupResult Resolve(SIPURI sipURI, bool async)
            {
                // This assumes the input SIP URI has an IP address as the host!
                return new SIPDNSLookupResult(sipURI, new SIPEndPoint(SIPSorcery.Sys.IPSocket.ParseSocketString(sipURI.Host)));
            }
        }

        private SIPRequest GetDummyINVITERequest(SIPURI dummyURI)
        {
            string dummyFrom = "<sip:unittest@mysipswitch.com>";
            string dummyContact = "sip:127.0.0.1:1234";
            SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, dummyURI);

            SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader(dummyFrom), new SIPToHeader(null, dummyURI, null), 1, CallProperties.CreateNewCallId());
            inviteHeader.From.FromTag = CallProperties.CreateNewTag();
            inviteHeader.Contact = SIPContactHeader.ParseContactHeader(dummyContact);
            inviteHeader.CSeqMethod = SIPMethodsEnum.INVITE;
            inviteHeader.UserAgent = "unittest";
            inviteRequest.Header = inviteHeader;

            SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 1234, CallProperties.CreateBranchId(), SIPProtocolsEnum.udp);
            inviteRequest.Header.Vias.PushViaHeader(viaHeader);

            inviteRequest.Body = "dummy";
            inviteRequest.Header.ContentLength = inviteRequest.Body.Length;
            inviteRequest.Header.ContentType = "application/sdp";

            return inviteRequest;
        }

        private DialPlanLineContext GetDummyDialPlanContext(string testDialPlan, string dst)
        {
            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);
            SIPTransactionEngine transactionEngine = new SIPTransactionEngine();
            SIPTransport sipTransport = new SIPTransport(MockSIPDNSManager.Resolve, transactionEngine);
            SIPURI dummyURI = SIPURI.ParseSIPURI(dst);
            SIPRequest inviteRequest = GetDummyINVITERequest(dummyURI);
            SIPEndPoint dummyEndPoint = SIPEndPoint.ParseSIPEndPoint("udp:0.0.0.0:5060");
            UASInviteTransaction uasTransaction = sipTransport.CreateUASTransaction(inviteRequest, dummyEndPoint, dummyEndPoint, null);
            SIPServerUserAgent uas = new SIPServerUserAgent(sipTransport, null, "test", "sipsorcery.com", SIPCallDirection.In, null, null, null, uasTransaction);
            DialPlanLineContext dialPlanContext = new DialPlanLineContext(null, null, null, null, uas, dialPlan, null, null, null, null);

            return dialPlanContext;
        }

        [TestMethod]
        public void SimpleDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = @"
					exten => 100,1,Switch(""anonymous.invalid"", ""password"", ""612@freeworlddialup.com"")
					exten => 101,1,Switch(""username"", ""password"", ""303@sip.blueface.ie"")
				";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);
            DialPlanLineContext dialPlanContext = new DialPlanLineContext(null, null, null, null, null, dialPlan, null, null, null, null);

            Console.WriteLine("dst=" + dialPlanContext.m_commands[0].Destination + ", data=" + dialPlanContext.m_commands[0].Data + ".");
            Console.WriteLine("dst=" + dialPlanContext.m_commands[1].Destination + ", data=" + dialPlanContext.m_commands[1].Data + ".");

            Assert.IsTrue(dialPlanContext.m_commands.Count == 2, "The dial plan was not correctly parsed.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Operation == DialPlanOpsEnum.Equals, "Command 1 oeration not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[1].Operation == DialPlanOpsEnum.Equals, "Command 2 oeration not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Destination == "100", "Command 1 destination not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[1].Destination == "101", "Command 2 destination not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Command == "Switch", "Command 1 command not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[1].Command == "Switch", "Command 2 command not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Data == "\"anonymous.invalid\", \"password\", \"612@freeworlddialup.com\"", "Command 1 data not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[1].Data == "\"username\", \"password\", \"303@sip.blueface.ie\"", "Command 2 data not correct.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void CommentOnLineTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = @"
					exten => 100,1,Switch(anonymous.invalid, password, 612@freeworlddialup.com) ; Comment
					exten => 101,1,Switch(""username"", ""password"", ""303@sip.blueface.ie)
				";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:dummy@mysipswitch.com");

            Console.WriteLine("dst=" + dialPlanContext.m_commands[0].Destination + ", data=" + dialPlanContext.m_commands[0].Data + ".");
            Console.WriteLine("dst=" + dialPlanContext.m_commands[1].Destination + ", data=" + dialPlanContext.m_commands[1].Data + ".");

            Assert.IsTrue(dialPlanContext.m_commands.Count == 2, "The dial plan was not correctly parsed.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Command == "Switch", "The dial plan command was not correct.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Data == "anonymous.invalid, password, 612@freeworlddialup.com", "The dial plan data was not correct.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void DifferentOperatorsDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = @"
					exten == 100,1,Switch(""anonymous.invalid"", ""password"", ""612@freeworlddialup.com"")
					exten =~ 101,1,Switch(""username"", ""password"", ""303@sip.blueface.ie"")
					exten = 103,1,Switch(""username"", ""password"", ""303@sip.blueface.ie)
				";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:dummy@mysipswitch.com");

            Assert.IsTrue(dialPlanContext.m_commands.Count == 3, "The dial plan was not correctly parsed.");
            Assert.IsTrue(dialPlanContext.m_commands[0].Operation == DialPlanOpsEnum.Equals, "Command 1 operation was incorrect.");
            Assert.IsTrue(dialPlanContext.m_commands[1].Operation == DialPlanOpsEnum.Regex, "Command 2 operation was incorrect.");
            Assert.IsTrue(dialPlanContext.m_commands[2].Operation == DialPlanOpsEnum.Equals, "Command 3 operation was incorrect.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void NoMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = @"
					exten => _3100,1,Switch(anon, password, 1@sip.blueface.ie)
					exten => _3300,1,Switch(anon, password, 2@sip.blueface.ie)
					exten => _3000,1,Switch(anon, password, 3@sip.blueface.ie)
				";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:3200@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("3200");

            Assert.IsTrue(dialPlanContext.m_commands.Count == 3, "The dial plan was not correctly parsed.");
            Assert.IsNull(commandMatch, "The dial plan produced a match when it should not have.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void NMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = @"
					exten => _3100,1,Switch(anon, password, 1@sip.blueface.ie)
					exten => _3N00,1,Switch(anon, password, 2@sip.blueface.ie)
					exten => _3000,1,Switch(anon, password, 3@sip.blueface.ie)
				";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:3200@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("3200");

            Assert.IsTrue(dialPlanContext.m_commands.Count == 3, "The dial plan was not correctly parsed.");
            Assert.IsTrue(commandMatch.Data == "anon, password, 2@sip.blueface.ie", "The dial plan command match was not correct.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void ZMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = @"
					exten => _3000,1,Switch(anon, password, 1@sip.blueface.ie)
					exten => _3001,1,Switch(anon, password, 2@sip.blueface.ie)
					exten => _3Z00,1,Switch(anon, password, 3@sip.blueface.ie)
				";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:3100@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("3100");

            Assert.IsTrue(dialPlanContext.m_commands.Count == 3, "The dial plan was not correctly parsed.");
            Assert.IsTrue(commandMatch.Data == "anon, password, 3@sip.blueface.ie", "The dial plan command match was not correct.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SingleXMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3X.,1,Switch(anon, password, 1@sip.blueface.ie)";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:300@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("300");

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SingleZMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3Z.,1,Switch(anon, password, 1@sip.blueface.ie)";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:310@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("310");

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SingleNMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3N.,1,Switch(anon, password, 1@sip.blueface.ie)";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:320@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("320");

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SingleRangeMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3[2-57-9].,1,Switch(anon, password, 1@sip.blueface.ie)";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:380@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("380");

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void SingleRangeNoMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3[2-57-9].,1,Switch(anon, password, 1@sip.blueface.ie)";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);
            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:360@sip.mysipswitch.com"));
            DialPlanLineContext dialPlanContext = new DialPlanLineContext(null, null, null, null, null, dialPlan, null, null, null, null);
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch(request.URI.User);

            Assert.IsNull(commandMatch, "The dial plan should not have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void MutliRangeMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3[2-57-9]X[1-3],1,Switch(anon, password, 1@sip.blueface.ie)";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:3802@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("3802");

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void MutliRangeNoMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan = "exten => _3[2-57-9]X[1-3].,1,Switch(anon, password, 1@sip.blueface.ie)";

            var dialPlanContext = GetDummyDialPlanContext(testDialPlan, "sip:3807@sip.mysipswitch.com");
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch("3807");

            Assert.IsNull(commandMatch, "The dial plan should not have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void TestLoadOldDefaultDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan =
                "; Example extensions\n" +
                "exten = 100,1,Switch(anon,,303@sip.blueface.ie)\n" +
                "exten == 101,1,Switch(anon,,612@fwd.pulver.com)\n" +
                "exten => _*1X.,1,Switch(user,pass,${EXTEN:2}@sip.blueface.ie)\n" +
                "exten => _*2X.,1,Switch(anon,,${EXTEN:2}@fwd.pulver.com)\n";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);

            Assert.IsNotNull(dialPlan, "The default dial plan could not be loaded.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void RegexWithCommaLoadDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan =
                "exten =~ /3d{4,6}/,1,Switch(anon,,${dst}@sip.blueface.ie, \"sip switch\" <sip:anon@sip.mysipswitch.com>, 194.213.29.100)";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);

            Assert.IsNotNull(dialPlan, "The dial plan could not be loaded.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void RegexWithCommaMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan =
                @"exten =~ /\d{3,6}/,1,Switch(anon,,${dst}@sip.blueface.ie, ""sip switch"" <sip:anon@sip.mysipswitch.com>, 194.213.29.100)";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);
            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            DialPlanLineContext dialPlanContext = new DialPlanLineContext(null, null, null, null, null, dialPlan, null, null, null, null);
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch(request.URI.User);

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void RegexWithCommaNoMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan =
                @"exten =~ /\D{3,6}/,1,Switch(anon,,${dst}@sip.blueface.ie, ""sip switch"" <sip:anon@sip.mysipswitch.com>, 194.213.29.100)";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);
            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            DialPlanLineContext dialPlanContext = new DialPlanLineContext(null, null, null, null, null, dialPlan, null, null, null, null);
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch(request.URI.User);

            Assert.IsNull(commandMatch, "The dial plan should not have returned a match.");

            Console.WriteLine("---------------------------------");
        }

        [TestMethod]
        public void RegexWithNoCommaMatchDialPlanTest()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            string testDialPlan =
                @"exten =~ \d{3},1,Switch(anon,,${dst}@sip.blueface.ie, ""sip switch"" <sip:anon@sip.mysipswitch.com>, 194.213.29.100)";

            SIPDialPlan dialPlan = new SIPDialPlan(null, null, null, testDialPlan, SIPDialPlanScriptTypesEnum.Asterisk);
            SIPRequest request = new SIPRequest(SIPMethodsEnum.INVITE, SIPURI.ParseSIPURI("sip:380@sip.mysipswitch.com"));
            DialPlanLineContext dialPlanContext = new DialPlanLineContext(null, null, null, null, null, dialPlan, null, null, null, null);
            DialPlanCommand commandMatch = dialPlanContext.GetDialPlanMatch(request.URI.User);

            Assert.IsNotNull(commandMatch, "The dial plan should have returned a match.");

            Console.WriteLine("---------------------------------");
        }
    }
}
