using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    class Thruster : Module<IMyThrust> {
        
        public IMyThrust mEngine;

        public Thruster(IMyThrust aEngine) {
            mEngine = aEngine;
        }


    }
}
