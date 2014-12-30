        [TestMethod]
        public async Task SendReturnsConditionalResponseOnOk()
        {
            var httpClient = Substitute.For<IHttpClient>();
            var cache = Substitute.For<ICache>();

            var cachingClient = new CachingHttpClient(httpClient, cache);

            var request = new Request { Method = HttpMethod.Get, Endpoint = new Uri("test", UriKind.Relative) };

            var cachedResponse = Substitute.For<IResponse<string>>();
            cachedResponse.ApiInfo.Returns(new ApiInfo(new Dictionary<string, Uri>(), new List<string>(), new List<string>(), "ABC123", null));

            cache.GetAsync<IResponse<string>>("test").Returns(Task.FromResult(cachedResponse));

            var conditionalResponse = Substitute.For<IResponse<string>>();
            conditionalResponse.StatusCode = HttpStatusCode.OK;

            httpClient.Send<string>(request, CancellationToken.None).Returns(Task.FromResult(conditionalResponse));

            var actualResponse = await cachingClient.Send<string>(request, CancellationToken.None);

            Assert.AreEqual(conditionalResponse, actualResponse);
        }
