using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    class Thrust : Module<IMyThrust> {
        public IMyThrust mEngine;
        public Group mGroup;
        public enum Group {
            Fuel,
            Ion,
            Air,
            Not
        }
        public Thrust(IMyThrust aEngine, Group aType) {
            mEngine = aEngine;
            mGroup = aType;
        }
    }
}
