using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using System.Threading.Tasks;
using UnityEngine;

public class G2C_RepeatLoginHandler : Message<G_2C_RepeaLogin>
{
    protected override async FTask Run(Session session, G_2C_RepeaLogin message)
    {
        Log.Info("”–»À¿¥∂•∫≈");
        await FTask.CompletedTask;
    }
}
