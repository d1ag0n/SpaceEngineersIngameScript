using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    public delegate void IGCHandler(Envelope m);
    public struct Envelope {
        public readonly double Time;
        public readonly MyIGCMessage Message;
        public Envelope(double aTime, MyIGCMessage aMessage) {
            Time = aTime;
            Message = aMessage;
            
        }
    }
    public class GridComModule : Module<IMyRadioAntenna> {
        //readonly ModuleManager mManager;
        readonly IMyIntergridCommunicationSystem mIGC;
        readonly SubscriptionManager mUnicastMgr;
        readonly SubscriptionManager mBroadcastMgr;
        readonly List<IMyBroadcastListener> mListeners = new List<IMyBroadcastListener>();

        bool mUpdateNeeded;
        public GridComModule(ModuleManager aManager) :base(aManager) {
            mIGC = aManager.mProgram.IGC;            
            mUnicastMgr = new SubscriptionManager(this);
            mBroadcastMgr = new SubscriptionManager(this);
            onIGC = MailCall;
            onUpdate = Update;
            Active = true;
        }
        public void SubscribeUnicast(string tag, IGCHandler h) {
            if (mUnicastMgr.Subscribe(tag, h))
                mIGC.UnicastListener.SetMessageCallback("UNICAST");
        }
        public void SubscribeBroadcast(string tag, IGCHandler h) {
            if (mBroadcastMgr.Subscribe(tag, h)) {
                var listener = mIGC.RegisterBroadcastListener(tag);
                listener.SetMessageCallback(tag);
                mListeners.Add(listener);
            }
        }
        void Update() {
            if (mUpdateNeeded) {
                mUnicastMgr.Update();
                mBroadcastMgr.Update();
                mUpdateNeeded = false;
            }
        }
        public void MailCall(double aTime) {
            mUnicastMgr.MailCall(mIGC.UnicastListener, aTime);
            foreach (var listener in mListeners) {
                mBroadcastMgr.MailCall(listener, aTime);
            }
        }
        class SubscriptionManager {
            readonly List<Envelope> mInbox = new List<Envelope>();
            readonly Dictionary<string, List<IGCHandler>> mSubscriptions = new Dictionary<string, List<IGCHandler>>();
            readonly GridComModule mCom;
            public SubscriptionManager(GridComModule aCom) {
                mCom = aCom;
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
                    mCom.mUpdateNeeded = true;
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
                    }
                }
                mInbox.Clear();
            }
        }
        
    }
}
