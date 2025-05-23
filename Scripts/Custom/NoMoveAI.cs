using Server;
using Server.Mobiles;
using System;

namespace Server.Mobiles
{
    public class NoMoveAI : BaseAI
    {
        public NoMoveAI(BaseCreature m) : base(m)
        {
        }

        public override bool DoActionWander()
        {
            return true;
        }

        public override bool DoActionCombat()
        {
            return true;
        }

        public override bool DoActionGuard()
        {
            return true;
        }

        public override bool DoActionFlee()
        {
            return true;
        }

        public override bool DoActionInteract()
        {
            return true;
        }

        public override bool DoActionBackoff()
        {
            return true;
        }

        public override bool DoOrderNone()
        {
            return true;
        }

        public override bool DoOrderFollow()
        {
            return true;
        }

        public override bool DoOrderCome()
        {
            return true;
        }

        public override bool DoOrderStay()
        {
            return true;
        }

        public override bool DoOrderStop()
        {
            return true;
        }

        public override bool DoOrderAttack()
        {
            return true;
        }

        public bool DoOrderGo()
        {
            return true;
        }

        public void OnThink()
        {
        }
    }
}
