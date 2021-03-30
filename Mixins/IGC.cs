using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {

    public class IGC {
        readonly ModuleManager mManager;
        
        readonly SubscriptionManager mUnicastMgr;
        readonly SubscriptionManager mBroadcastMgr;
        readonly List<IMyBroadcastListener> mListeners = new List<IMyBroadcastListener>();

        bool mUpdateNeeded;
        public IGC(ModuleManager aManager) {
            mManager = aManager;
            
            mUnicastMgr = new SubscriptionManager(this);
            mBroadcastMgr = new SubscriptionManager(this);
        }
        public void SubscribeUnicast(string tag, IGCHandler h) {
            if (mUnicastMgr.Subscribe(tag, h))
                mManager.mProgram.IGC.UnicastListener.SetMessageCallback("UNICAST");
        }
        public void SubscribeBroadcast(string tag, IGCHandler h) {
            if (mBroadcastMgr.Subscribe(tag, h)) {
                var listener = mManager.mProgram.IGC.RegisterBroadcastListener(tag);
                listener.SetMessageCallback(tag);
                mListeners.Add(listener);
                mManager.logger.persist($"Callback set for: {tag}");
            }
        }
        public void Update() {
            if (mUpdateNeeded) {
                mUnicastMgr.Update();
                mBroadcastMgr.Update();
                mUpdateNeeded = false;
            }
        }
        public void MailCall(double aTime) {
            mUnicastMgr.MailCall(mManager.mProgram.IGC.UnicastListener, aTime);
            foreach (var listener in mListeners) {
                mBroadcastMgr.MailCall(listener, aTime);
            }
        }
        class SubscriptionManager {
            readonly List<Envelope> mInbox = new List<Envelope>();
            readonly Dictionary<string, List<IGCHandler>> mSubscriptions = new Dictionary<string, List<IGCHandler>>();
            readonly IGC mIGC;
            public SubscriptionManager(IGC aIGC) {
                mIGC = aIGC;
            }
            public bool Subscribe(string tag, IGCHandler h) {
                var result = false;
                List<IGCHandler> list;
                if (!mSubscriptions.TryGetValue(tag, out list)) {
                    list = new List<IGCHandler>();
                    mSubscriptions.Add(tag, list);
                    result = true;
                }
                list.Add(h);
                return result;
            }
            public void MailCall(IMyMessageProvider aProvider, double aTime) {
                while (aProvider.HasPendingMessage) {
                    mIGC.mUpdateNeeded = true;
                    mInbox.Add(new Envelope(aTime, aProvider.AcceptMessage()));
                }
            }

            public void Update() {
                foreach (var envelope in mInbox) {
                    List<IGCHandler> list;
                    if (mSubscriptions.TryGetValue(envelope.Message.Tag, out list)) {
                        foreach (var handler in list) {
                            handler(envelope);
                        }
                    } else {
                        mIGC.mManager.logger.persist($"No handlers found.");
                    }
                }
                mInbox.Clear();
            }
        }
        public struct Envelope {
            public readonly double Time;
            public readonly MyIGCMessage Message;
            public Envelope(double aTime, MyIGCMessage aMessage) {
                Time = aTime;
                Message = aMessage;
            }
        }
    }
}