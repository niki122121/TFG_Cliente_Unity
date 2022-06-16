using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataChannelParams : MonoBehaviour
{
    public string channelName;
    public int updateTimerMs = 200;
    public bool ordered;
    public int maxRetransmits;
    public bool handleSendQueueOneByOne = false;
    public bool commandBased;
}
