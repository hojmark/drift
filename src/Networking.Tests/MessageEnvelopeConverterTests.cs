using Drift.Domain;
using Drift.Networking.Core.Messages;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.Tests.Helpers;

namespace Drift.Networking.Tests;

internal sealed class MessageEnvelopeConverterTests {
  private readonly MessageEnvelopeConverter _converter = new();

  [Test]
  public void RequestEnvelope_ContainsRequestIdOnly() {
    var requestId = RequestId.New();
    var envelope = _converter.ToEnvelope<TestMessage, TestMessage>(
      new TestMessage { Payload = "request" },
      requestId
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( envelope.RequestId, Is.EqualTo( requestId.ToString() ) );
      Assert.That( envelope.ReplyTo, Is.Empty );
    }
  }

  [Test]
  public void ResponseEnvelope_ContainsReplyToOnly() {
    var requestId = RequestId.New();
    var envelope = _converter.ToEnvelope( new TestMessage { Payload = "response" }, requestId );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( envelope.RequestId, Is.Empty );
      Assert.That( envelope.ReplyTo, Is.EqualTo( requestId.ToString() ) );
    }
  }

  [Test]
  public void FromEnvelope_RejectsBothLinkageFields() {
    var requestId = RequestId.New().ToString();
    var envelope = new Message {
      MessageType = TestMessage.MessageType, Payload = "payload", RequestId = requestId, ReplyTo = requestId
    };

    Assert.Throws<InvalidOperationException>( () =>
      _converter.FromResponseEnvelope<TestMessage>( envelope )
    );
  }

  [Test]
  public void FromEnvelope_RejectsMissingLinkageFields() {
    var envelope = new Message { MessageType = TestMessage.MessageType, Payload = "{}" };

    Assert.Throws<InvalidOperationException>( () =>
      _converter.FromRequestEnvelope<TestMessage, TestMessage>( envelope )
    );
  }
}