(() => {

    const serverTimeout = 30000;
    const keepAliveInterval = 2000;

    const maximumRetryCount = 40;
    const retryIntervalMilliseconds = 2000;
  //  const reconnectModal = document.getElementById('reconnect-modal');
    const reconnectModal = document.getElementById('components-reconnect-modal');

    const startReconnectionProcess = () => {
        console.log('Connection down. Starting reconnection process.');
        reconnectModal.style.display = 'block';

        let isCanceled = false;

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                reconnectModal.innerText = `Attempting to reconnect: ${i + 1} of ${maximumRetryCount}`;

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

                if (isCanceled) {
                    return;
                }

                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server was reached, but the connection was rejected; reload the page.
                        location.reload();
                        return;
                    }

                    // Successfully reconnected to the server.
                    return;
                } catch {
                    // Didn't reach the server; try again.
                }
            }

            // Retried too many times; reload the page.
            location.reload();
        })();

        return {
            cancel: () => {
                isCanceled = true;
                reconnectModal.style.display = 'none';
            },
        };
    };

    let currentReconnectionProcess = null;

    Blazor.start({
        //circuit: {
        //    configureSignalR: function (builder) {
        //        builder.withServerTimeout(serverTimeout).withKeepAliveInterval(keepAliveInterval);
        //    }
        //},
        reconnectionHandler: {
            onConnectionDown: () => currentReconnectionProcess ??= startReconnectionProcess(),
            onConnectionUp: () => {
                currentReconnectionProcess?.cancel();
                currentReconnectionProcess = null;
            }
        }
    });
})();