using System;
using System.Net;
using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proshot.CommandClient;
using Rhino.Mocks;
using System.Linq;
using System.Threading;

namespace CommandClientVisualStudioTest
{
    [TestClass]
    public class AdvancedMockTests
    {
        private MockRepository mocks;
        

        [TestMethod]
        public void VerySimpleTest()
        {
            CMDClient client = new CMDClient(null, "Bogus network name");
            Assert.AreEqual("Bogus network name", client.NetworkName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            mocks = new MockRepository();
        }

        [TestMethod]
        public void TestUserExitCommand()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            //Use of reflection
            // we need to set the private variable here
            Type typeofClient = client.GetType();
            FieldInfo stream = typeofClient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance); // can get a specific field
            stream.SetValue(client, fakeStream);   //set the value of network stream in the instance of the CommandClient to the fake stream

            bool result = client.SendCommandToServerUnthreaded(command);

            mocks.VerifyAll();
            Assert.IsTrue(result);

        }

        [TestMethod]
        public void TestUserExitCommandWithoutMocks()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            // MemoryStream memStream = new MemoryStream();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };
            //Memory Stream

            System.IO.MemoryStream streamIn = new MemoryStream(100);
            System.IO.MemoryStream receiveOut = new MemoryStream(100);
            streamIn.Write(commandBytes, 0, 4);
            streamIn.Flush();
            streamIn.Write(ipLength, 0, 4);
            streamIn.Flush();
            streamIn.Write(ip, 0, 9);
            streamIn.Flush();
            streamIn.Write(metaDataLength, 0, 4);
            streamIn.Flush();
            streamIn.Write(metaData, 0, 2);
            streamIn.Flush();
            
            CMDClient client = new CMDClient(null, "Bogus network name");
            //Use of reflection
            // we need to set the private variable here
            Type typeofClient = client.GetType();
            FieldInfo stream = typeofClient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance); // can get a specific field
            stream.SetValue(client, receiveOut);   //set the value of network stream in the instance of the CommandClient to the fake stream

            bool result= client.SendCommandToServerUnthreaded(command);
            //check the output stream

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(streamIn.ToArray(), receiveOut.ToArray());

        }


        [TestMethod]
        public void TestSemaphoreReleaseOnNormalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();

            using (mocks.Ordered()) {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                Expect.Call(fakeSemaphore.Release()).Return(1);
            }
            mocks.ReplayAll();

            CMDClient client = new CMDClient(null, "Bogus network name");
            //Use of reflection
            Type typeofClient = client.GetType();
            FieldInfo stream = typeofClient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance); // can get a specific field
            stream.SetValue(client, fakeStream);   //set the value of network stream in the instance of the CommandClient to the fake stream
            FieldInfo sem = typeofClient.GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance); // can get a specific field
            sem.SetValue(client, fakeSemaphore);

            bool result = client.SendCommandToServerUnthreaded(command);
            
            mocks.VerifyAll();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnExceptionalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();

            //in your mocks.Ordered block
            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                Expect.Call(fakeSemaphore.Release()).Return(1);
                fakeStream.Flush();
                LastCall.On(fakeStream).Throw(new Exception());
            }

            CMDClient client = new CMDClient(null, "Bogus network name");
            //Use of reflection
            Type typeofClient = client.GetType();
            FieldInfo stream = typeofClient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance); // can get a specific field
            stream.SetValue(client, fakeStream);   //set the value of network stream in the instance of the CommandClient to the fake stream
            FieldInfo sem = typeofClient.GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance); // can get a specific field
            sem.SetValue(client, fakeSemaphore);
            Boolean caughtException=false;
            try { 
                client.SendCommandToServerUnthreaded(command); 
            } catch (Exception e) {
                caughtException = true;
            }
            if (caughtException == false)
            {
                mocks.VerifyAll();
            }
            Assert.IsTrue(caughtException, "caught exception");

        }
    }
}
