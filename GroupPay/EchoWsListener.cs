using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Core;
using Microsoft.AspNetCore.Mvc;

namespace GroupPay
{
    [Route("/ws/echo")]
    public class EchoWsListener : IWebSocketListener
    {
        public Task OnBinaryMessage(WebSocketSession session, byte[] message)
        {
            Console.WriteLine("{0} received a binary message with length {1}", session.RemoteAddr, message.Length);
            return Task.CompletedTask;
        }

        public Task OnClosed(WebSocketSession session, WebSocketCloseStatus status, string reason)
        {
            Console.WriteLine("{0} closed", session.RemoteAddr);
            return Task.CompletedTask;
        }

        public Task OnConnected(WebSocketSession session)
        {
            Console.WriteLine("{0} connected", session.RemoteAddr);
            return Task.CompletedTask;
        }

        public Task OnError(WebSocketSession session, Exception exception)
        {
            Console.WriteLine("{0} faulted {1}", session.RemoteAddr, exception);
            return Task.CompletedTask;
        }

        public async Task OnTextMessage(WebSocketSession session, string message)
        {
            Console.WriteLine("receives amessage {0} from {1}", message, session.RemoteAddr);
            if (message == "bye")
            {
                await session.Close(WebSocketCloseStatus.NormalClosure, "bye");
            }
            else
            {
                await session.SendText(message);
            }
        }
    }
}
