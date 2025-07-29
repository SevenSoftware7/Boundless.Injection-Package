using System;
using Moq;
using Xunit;

namespace Seven.Boundless.Injection.Tests;
public class InjectionExtensionsTest {
	[Fact]
	public void PropagateInjection_ShouldLogPropagation() {
		// Arrange
		Mock<IInjectionNode> mockNode = new();
		Mock<IInjector<string>> mockInjector = new();

		Mock<Action<string>> mockLogger = new();

		mockInjector.Setup(i => i.InjectionNode).Returns(mockNode.Object);
		mockInjector.Setup(i => i.GetInjectValue()).Returns("testValue");
		mockNode.Setup(n => n.NodeName).Returns("TestNode");
		mockNode.Setup(n => n.Children).Returns([]);

		// Act
		mockInjector.Object.PropagateInjection(mockLogger.Object);

		// Assert
		mockLogger.Verify(l => l.Invoke("[Boundless.Injection] : Propagating testValue (type String) to TestNode children"), Times.Once);
	}

	[Fact]
	public void PropagateInjection_ShouldPropagateToChildren() {
		// Arrange
		Mock<IInjectionNode> mockRootNode = new();
		Mock<IInjector<string>> mockInjector = new();

		Mock<IInjectionNode> mockChildNode = new();
		Mock<IInjectable<string>> mockInjectable = new();

		Mock<Action<string>> mockLogger = new();

		mockInjector.Setup(i => i.InjectionNode).Returns(mockRootNode.Object);
		mockInjector.Setup(i => i.GetInjectValue()).Returns("testValue");
		mockRootNode.Setup(n => n.NodeName).Returns("RootNode");
		mockRootNode.Setup(n => n.Children).Returns([mockChildNode.Object]);

		mockChildNode.Setup(n => n.UnderlyingObject).Returns(mockInjectable.Object);

		// Act
		mockInjector.Object.PropagateInjection(mockLogger.Object);

		// Assert
		mockInjectable.Verify(i => i.Inject("testValue"), Times.Once);
	}


	[Fact]
	public void PropagateInjection_ShouldSkipBlockedChildren() {
		// Arrange
		Mock<IInjectionNode> mockRootNode = new();
		Mock<IInjector<string>> mockInjector = new();

		Mock<IInjectionNode> mockParentNode = new();
		Mock<IInjectionBlocker<string>> mockBlocker = new();

		Mock<IInjectionNode> mockBlockedChildNode = new();
		Mock<IInjectable<string>> mockBlockedInjectable = new();

		Mock<IInjectionNode> mockUnblockedChildNode = new();
		Mock<IInjectable<string>> mockUnblockedInjectable = new();

		Mock<Action<string>> mockLogger = new();

		mockRootNode.Setup(n => n.NodeName).Returns("RootNode");
		mockRootNode.Setup(n => n.Children).Returns([mockParentNode.Object]);
		mockInjector.Setup(i => i.InjectionNode).Returns(mockRootNode.Object);
		mockInjector.Setup(i => i.GetInjectValue()).Returns("testValue");

		mockBlocker.Setup(b => b.ShouldBlock(mockBlockedChildNode.Object, It.IsAny<string>())).Returns(true);
		mockBlocker.Setup(b => b.ShouldBlock(mockUnblockedChildNode.Object, It.IsAny<string>())).Returns(false);
		mockParentNode.Setup(n => n.NodeName).Returns("ParentNode");
		mockParentNode.Setup(n => n.Children).Returns([mockBlockedChildNode.Object, mockUnblockedChildNode.Object]);
		mockParentNode.Setup(n => n.UnderlyingObject).Returns(mockBlocker.Object);

		mockBlockedChildNode.Setup(n => n.UnderlyingObject).Returns(mockBlockedInjectable.Object);
		mockUnblockedChildNode.Setup(n => n.UnderlyingObject).Returns(mockUnblockedInjectable.Object);

		// Act
		mockInjector.Object.PropagateInjection(mockLogger.Object);

		// Assert
		mockBlockedInjectable.Verify(i => i.Inject("testValue"), Times.Never);
		mockUnblockedInjectable.Verify(i => i.Inject("testValue"), Times.Once);
	}

	[Fact]
	public void PropagateInjection_ShouldInterceptAndModifyValue() {
		// Arrange
		Mock<IInjectionNode> mockRootNode = new();
		Mock<IInjector<string>> mockInjector = new();

		Mock<IInjectionNode> mockParentNode = new();
		Mock<IInjectionInterceptor<string>> mockInterceptor = new();

		Mock<IInjectionNode> mockModifiedChildNode = new();
		Mock<IInjectable<string>> mockModifiedInjectable = new();

		Mock<IInjectionNode> mockUnmodifiedChildNode = new();
		Mock<IInjectable<string>> mockUnmodifiedInjectable = new();

		Mock<Action<string>> mockLogger = new();

		mockRootNode.Setup(n => n.NodeName).Returns("RootNode");
		mockRootNode.Setup(n => n.Children).Returns([mockParentNode.Object]);
		mockInjector.Setup(i => i.InjectionNode).Returns(mockRootNode.Object);
		mockInjector.Setup(i => i.GetInjectValue()).Returns("testValue");

		mockInterceptor.Setup(b => b.Intercept(mockModifiedChildNode.Object, It.IsAny<string>())).Returns("modifiedValue");
		mockInterceptor.Setup(b => b.Intercept(mockUnmodifiedChildNode.Object, It.IsAny<string>())).Returns("testValue");
		mockParentNode.Setup(n => n.NodeName).Returns("ParentNode");
		mockParentNode.Setup(n => n.Children).Returns([mockModifiedChildNode.Object, mockUnmodifiedChildNode.Object]);
		mockParentNode.Setup(n => n.UnderlyingObject).Returns(mockInterceptor.Object);

		mockModifiedChildNode.Setup(n => n.UnderlyingObject).Returns(mockModifiedInjectable.Object);
		mockUnmodifiedChildNode.Setup(n => n.UnderlyingObject).Returns(mockUnmodifiedInjectable.Object);

		// Act
		mockInjector.Object.PropagateInjection(mockLogger.Object);

		// Assert
		mockModifiedInjectable.Verify(i => i.Inject("modifiedValue"), Times.Once);
		mockUnmodifiedInjectable.Verify(i => i.Inject("testValue"), Times.Once);
	}

	[Fact]
	public void RequestInjection_ShouldLogRequest() {
		// Arrange
		Mock<IInjectionNode> mockNode = new();
		Mock<IInjectable<string>> mockRequester = new();

		Mock<IInjectionNode> mockParentNode = new();

		Mock<Action<string>> mockLogger = new();

		mockNode.Setup(n => n.Parent).Returns(mockParentNode.Object);
		mockNode.Setup(n => n.IsTreeReady).Returns(true);
		mockRequester.Setup(r => r.InjectionNode).Returns(mockNode.Object);

		// Act
		mockRequester.Object.RequestInjection(logger: mockLogger.Object);

		// Assert
		mockLogger.Verify(l => l.Invoke("[Boundless.Injection] : Requesting Injection of String"), Times.Once);
	}

	[Fact]
	public void RequestInjection_ShouldReturnFalseIfNoParent() {
		// Arrange
		Mock<IInjectionNode> mockNode = new();
		Mock<IInjectable<string>> mockRequester = new();

		mockNode.Setup(n => n.Parent).Returns((IInjectionNode)null);
		mockRequester.Setup(r => r.InjectionNode).Returns(mockNode.Object);

		// Act
		bool result = mockRequester.Object.RequestInjection<string>();

		// Assert
		Assert.False(result);
		mockRequester.Verify(r => r.Inject(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public void RequestInjection_ShouldReturnFalseIfParentNotReady() {
		// Arrange
		Mock<IInjectionNode> mockNode = new();
		Mock<IInjectable<string>> mockRequester = new();

		Mock<IInjectionNode> mockParentNode = new();

		mockNode.Setup(n => n.Parent).Returns(mockParentNode.Object);
		mockNode.Setup(n => n.IsTreeReady).Returns(false);
		mockRequester.Setup(r => r.InjectionNode).Returns(mockNode.Object);

		// Act
		bool result = mockRequester.Object.RequestInjection<string>();

		// Assert
		Assert.False(result);
		mockRequester.Verify(r => r.Inject(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public void RequestInjection_ShouldPropagateInjectionIfInjectorFound() {
		// Arrange
		Mock<IInjectionNode> mockRootNode = new();
		Mock<IInjector<string>> mockInjector = new();

		Mock<IInjectionNode> mockChildNode = new();
		Mock<IInjectable<string>> mockInjectable = new();

		Mock<Action<string>> mockLogger = new();

		mockChildNode.Setup(n => n.UnderlyingObject).Returns(mockInjectable.Object);
		mockChildNode.Setup(n => n.Parent).Returns(mockRootNode.Object);
		mockChildNode.Setup(n => n.IsTreeReady).Returns(true);
		mockInjectable.Setup(i => i.InjectionNode).Returns(mockChildNode.Object);

		mockRootNode.Setup(n => n.UnderlyingObject).Returns(mockInjector.Object);
		mockRootNode.Setup(n => n.Children).Returns([mockChildNode.Object]);
		mockInjector.Setup(i => i.InjectionNode).Returns(mockRootNode.Object);
		mockInjector.Setup(i => i.GetInjectValue()).Returns("testValue");

		// Act
		mockInjectable.Object.RequestInjection<string>();

		// Assert
		mockInjectable.Verify(i => i.Inject("testValue"), Times.Once);
	}

	[Fact]
	public void RequestInjection_ShouldAcceptNodeAsInjection() {
		// Arrange
		Mock<IInjectionNode> mockNode = new();
		Mock<IInjectable<string>> mockRequester = new();

		Mock<IInjectionNode> mockParentNode = new();

		Mock<Action<string>> mockLogger = new();

		mockRequester.Setup(r => r.InjectionNode).Returns(mockNode.Object);
		mockNode.Setup(n => n.Parent).Returns(mockParentNode.Object);
		mockNode.Setup(n => n.IsTreeReady).Returns(true);
		mockParentNode.Setup(n => n.UnderlyingObject).Returns("testValue");

		// Act
		bool result = mockRequester.Object.RequestInjection(acceptNodeAsInjection: true, logger: mockLogger.Object);

		// Assert
		Assert.True(result);
		mockLogger.Verify(l => l.Invoke("[Boundless.Injection] : Found String Injector"), Times.Once);
	}
}
