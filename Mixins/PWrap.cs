﻿using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;

namespace IngameScript
{
    class PWrap {

        const string coreTag = "coreTag";
        const string coreAssert = "coreAssert";
        const string coreQuery = "coreQuery";
        const string corePing = "corePing";
        const string corePong = "corePong";
        public readonly MyGridProgram pc;
        public readonly GTS gts;
        public readonly Logger g;
        bool initialized = false;
        bool wrapInitialized = false;
        TimeSpan time;
        long coreNext;
        long corePrevious;
        IMyBroadcastListener coreListener;
        IMyTextPanel mConsole;
        readonly List<MyIGCMessage> messages;
        readonly List<MyIGCMessage> coreMessages;
        double lastCorePing = 0;
        bool corePongReceived = true;
        bool coreBroadcastSent;
        const double pingInterval = 10.0;
        
        long id {
            get {
                return pc.Me.EntityId;
            }
        }
        bool isCore {
            get {
                return id > coreNext;
            }
        }

        public PWrap(MyGridProgram aProgram) {
            pc = aProgram;
            pc.Runtime.UpdateFrequency = UpdateFrequency.Update100;
            g = new Logger();
            gts = new GTS(pc, g);
            messages = new List<MyIGCMessage>();
            coreMessages = new List<MyIGCMessage>();
            time = TimeSpan.Zero;
        }
        protected virtual void handleMessage(MyIGCMessage aMessage) {
        }
        protected virtual bool init() {
            return true;
        }
        protected virtual int update(string argument, UpdateType aUpdate) {
            return 0;
        }
        private bool wrapInit() {
            coreListener = pc.IGC.RegisterBroadcastListener(coreTag);
            gts.getByTag("console", ref mConsole);
            g.persist("BC wrapInit");
            sendCoreBroadcast();
            return true;
        }
        protected bool getMessage(ref MyIGCMessage aMessage) {
            var result = false;
            if (messages.Count > 0) {
                aMessage = messages[0];
                messages.RemoveAt(0);
                result = true;
            }
            return result;
        }
        bool handleCoreMessage(MyIGCMessage aMessage) {
            var result = false;
            if (null != aMessage.Data) {
                result = handleCoreMessage(aMessage.Source, aMessage.Tag, aMessage.Data.ToString());
            }
            return result;
        }
        bool handleCoreMessage(long aSource, string aTag, string aData) {
            var result = false;
            switch (aData) {
                case coreAssert:
                    if (aSource > id) {
                        if (0 == coreNext) {
                            coreNext = aSource;
                            g.persist("BC coreNext = zero");
                            result = true;
                        } else if (aSource < coreNext) {
                            g.persist("BC source < coreNext");
                            coreNext = aSource;
                            result = true;
                        }
                    } else {
                        result = true;
                        if (0 == corePrevious) {
                            g.persist("BC corePrevious = zero");
                            corePrevious = aSource;
                            result = true;
                        } else if (aSource > corePrevious) {
                            g.persist("BC source > corePrevious");
                            corePrevious = aSource;
                        }
                    }
                    break;
                case corePing:
                    if (aSource == corePrevious) {
                        if (sendPong()) {
                            g.persist($"Pong sent @ {time.TotalSeconds}");
                        } else {
                            g.persist("BC fail to pong");
                            corePrevious = 0;
                            result = true;
                        }
                    } else {
                        g.persist($"Ignored ping from {aSource}");
                    }
                    break;
                case corePong:
                    if (aSource == coreNext) {
                        corePongReceived = true;
                    } else {
                        g.persist($"Ignored pong from {aSource}");
                    }
                    break;
            }
            return result;
        }
        bool sendPong() => pc.IGC.SendUnicastMessage(corePrevious, coreTag, corePong);
        void sendPing() {
            if (coreNext > 0 && (time.TotalSeconds - lastCorePing) > pingInterval) {
                lastCorePing = time.TotalSeconds;
                var send = !corePongReceived;
                if (send) {
                    coreNext = 0;
                    g.persist("BC pong not received");
                }
                corePongReceived = false;

                if (!pc.IGC.SendUnicastMessage(coreNext, coreTag, corePing)) {
                    send = true;
                    g.persist("BC ping send fail");
                    coreNext = 0;
                } else {
                    g.persist($"Ping sent @ {time.TotalSeconds}");
                }
                if (send) {
                    sendCoreBroadcast();
                }
            }
        }
        void sendCoreBroadcast() {
            if (!coreBroadcastSent) {
                corePongReceived = true;
                lastCorePing = time.TotalSeconds;
                pc.IGC.SendBroadcastMessage(coreTag, coreAssert);
                g.persist($"Core broadcast sent @ {time.TotalSeconds}");
                coreBroadcastSent = true;
            }
        }
        void handleMessages() {
            receiveUnicastMessages();
            receiveBroadcastMessages();
            var send = false;
            while (coreMessages.Count > 0) {
                var msg = coreMessages[0];
                coreMessages.RemoveAt(0);
                if (handleCoreMessage(msg)) {
                    send = true;
                }
            }
            if (send) {
                g.persist("BC handleMessages");
                sendCoreBroadcast();
            } else if (isCore) {
                if ((time.TotalSeconds - lastCorePing) > pingInterval) {
                    g.persist("BC isCore");
                    sendCoreBroadcast();
                }
            }
            if (initialized && messages.Count > 0) {
                var msg = messages[0];
                messages.RemoveAt(0);
                handleMessage(msg);
            }
        }
        void receiveBroadcastMessages() {
            while (coreListener.HasPendingMessage) {
                var msg = coreListener.AcceptMessage();
                if (coreTag == msg.Tag) {
                    coreMessages.Add(msg);
                } else {
                    messages.Add(msg);
                }
            }
            
        }
        void receiveUnicastMessages() {
            while (pc.IGC.UnicastListener.HasPendingMessage) {
                var msg = pc.IGC.UnicastListener.AcceptMessage();
                if (coreTag == msg.Tag) {
                    coreMessages.Add(msg);
                } else {
                    messages.Add(msg);
                }
            }
        }

        public void Main(string argument, UpdateType aUpdate) {
            time += pc.Runtime.TimeSinceLastRun;
            
            if (aUpdate.HasFlag(UpdateType.Update100)) {
                g.log(id);
                try {
                    coreBroadcastSent = pc.Me.CubeGrid.GridSizeEnum.HasFlag(MyCubeSize.Small);
                    if (wrapInitialized) {
                        handleMessages();
                        if (initialized) {
                            if (isCore) {
                                g.log($"{coreAssert} {id}");
                            } else {
                                g.log($"{id}");
                                g.log($"Next core {coreNext}");
                            }
                            g.log($"Previous core {corePrevious}");
                            sendPing();
                            update(argument, aUpdate);
                        } else {
                            initialized = init();
                        }
                    } else {
                        wrapInitialized = wrapInit();
                    }
                } catch (Exception ex) {
                    g.persist(ex.ToString());
                }
                g.log($"core uptime {time.TotalSeconds}");
                var log = g.clear();
                if (null != mConsole) {
                    mConsole.WriteText(log);
                }
                pc.Echo(log);
            }
        }
    }
}
