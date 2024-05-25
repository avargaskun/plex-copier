using System.Net;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using NUnit.Framework;
using PlexCopier.TvDb;

namespace tst
{
    public class RetryHandlerTests
    {
        [Test]
        public async Task ShouldApplyMultiplierWhenCallFailsAlwaysWith500()
        {
            var retries = 5;
            var request = Substitute.For<HttpRequestMessage>();
            var ct = CancellationToken.None;
            var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            var handler = Substitute.ForPartsOf<RetryHandler>(retries, TimeSpan.FromMilliseconds(100), 2, null);
            handler.Configure().AsyncDelay(default, default).ReturnsForAnyArgs(Task.FromResult(0));
            handler.InnerHandler = Substitute.For<HttpMessageHandler>();
            handler.InnerHandler.Protected("SendAsync", request, ct).Returns(Task.FromResult(errorResponse));
            
            using var invoker = new HttpMessageInvoker(handler);
            var response = await invoker.SendAsync(request, ct);

            Assert.That(response, Is.SameAs(errorResponse));
            handler.InnerHandler.Received(retries + 1).Protected("SendAsync", request, ct);
            _ = handler.Configure().Received().AsyncDelay(100, ct);
            _ = handler.Configure().Received().AsyncDelay(200, ct);
            _ = handler.Configure().Received().AsyncDelay(400, ct);
            _ = handler.Configure().Received().AsyncDelay(800, ct);
            _ = handler.Configure().Received().AsyncDelay(1600, ct);
        }

        [Test]
        public void ShouldApplyMultiplierWhenCallThrowsAlways()
        {
            var retries = 5;
            var request = Substitute.For<HttpRequestMessage>();
            var ct = CancellationToken.None;

            var handler = Substitute.ForPartsOf<RetryHandler>(retries, TimeSpan.FromMilliseconds(100), 2, null);
            handler.Configure().AsyncDelay(default, default).ReturnsForAnyArgs(Task.FromResult(0));
            handler.InnerHandler = Substitute.For<HttpMessageHandler>();
            handler.InnerHandler.Protected("SendAsync", request, ct).Throws<HttpRequestException>();
            
            using var invoker = new HttpMessageInvoker(handler);
            Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(request, ct));

            handler.InnerHandler.Received(retries + 1).Protected("SendAsync", request, ct);
            _ = handler.Configure().Received().AsyncDelay(100, ct);
            _ = handler.Configure().Received().AsyncDelay(200, ct);
            _ = handler.Configure().Received().AsyncDelay(400, ct);
            _ = handler.Configure().Received().AsyncDelay(800, ct);
            _ = handler.Configure().Received().AsyncDelay(1600, ct);
        }

        [Test]
        public async Task ShouldNotRetryWhenFirstRequestSuceeds()
        {
            var request = Substitute.For<HttpRequestMessage>();
            var ct = CancellationToken.None;
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            
            var handler = Substitute.ForPartsOf<RetryHandler>(5, TimeSpan.FromMilliseconds(100), 2, null);
            handler.Configure().AsyncDelay(default, default).ReturnsForAnyArgs(Task.FromResult(0));
            handler.InnerHandler = Substitute.For<HttpMessageHandler>();
            handler.InnerHandler.Protected("SendAsync", request, ct).Returns(Task.FromResult(successResponse));
            
            using var invoker = new HttpMessageInvoker(handler);

            await Assert.ThatAsync(() => invoker.SendAsync(request, ct), Is.SameAs(successResponse));
            handler.InnerHandler.Received(1).Protected("SendAsync", request, ct);
            _ = handler.Configure().DidNotReceiveWithAnyArgs().AsyncDelay(default, default);
        }

        [Test]
        public async Task ShouldRetryOnErrorsAndExceptionsButEventuallySucceeds()
        {
            var request = Substitute.For<HttpRequestMessage>();
            var ct = CancellationToken.None;
            var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            
            var handler = Substitute.ForPartsOf<RetryHandler>(5, TimeSpan.FromMilliseconds(100), 2, null);
            handler.Configure().AsyncDelay(default, default).ReturnsForAnyArgs(Task.FromResult(0));
            handler.InnerHandler = Substitute.For<HttpMessageHandler>();
            handler.InnerHandler.Protected("SendAsync", request, ct).Returns
            (
                x => Task.FromResult(errorResponse),
                x => Task.FromResult(errorResponse),
                x => { throw new HttpRequestException(); },
                x => Task.FromResult(successResponse)
            );
            
            using var invoker = new HttpMessageInvoker(handler);

            await Assert.ThatAsync(() => invoker.SendAsync(request, ct), Is.SameAs(successResponse));
            handler.InnerHandler.Received(4).Protected("SendAsync", request, ct);
            _ = handler.Configure().Received().AsyncDelay(100, ct);
            _ = handler.Configure().Received().AsyncDelay(200, ct);
            _ = handler.Configure().Received().AsyncDelay(400, ct);
        }
    }
}