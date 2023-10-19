export default {
	// The fetch handler is invoked when this worker receives a HTTP(S) request
	// and should return a Response (optionally wrapped in a Promise)
	async fetch(request, env) {
		const url = new URL(request.url);
		switch (url.pathname) {
			case '/':
				return singleOrderHandler(env);
			case '/sendOrders':
				return multipleOrderHandler(env);
			default:
				return new Response("Not found", { status: 404 })
		}
	},
};

async function singleOrderHandler(env) {
	const order = createOrder(1);
	const init = {
		body: JSON.stringify(order),
		method: "POST",
		headers: {
			"content-type": "application/json",
			'dapr-api-token': `${env.CHECKOUT_API_TOKEN}`
		},
	};

	const response = await fetch(getPublishUrl(env), init);
	let message;
	if (response.ok) {
		message = `Successfully published a message! Response: ${response.status} ${response.statusText} \n${JSON.stringify(order, null, ' ')}`;
	} else {
		message = `Unable to publish a message! Response: ${response.status} ${response.statusText}`;
	}
	return new Response(message);
}

function getPublishUrl(env) {
	return `${env.DAPR_HOST}:${env.DAPR_HTTP_PORT}/v1.0/publish/${env.PUBSUB_NAME}/${env.PUBSUB_TOPIC}`
}

function createOrder(orderId) {
	return {
		orderId: orderId,
		time: new Date().toISOString()
	}
}

async function multipleOrderHandler(env) {
	const numberOfOrders = 10;
	const sentOrders = [];
	for (let i = 1; i <= numberOfOrders; i++) {
		const order = createOrder(i);
		const request = {
			body: JSON.stringify(order),
			method: "POST",
			headers: {
				"content-type": "application/json",
				'dapr-api-token': `${env.CHECKOUT_API_TOKEN}`
			},
		};
		const response = await fetch(getPublishUrl(env), request);
		if (response.ok) {
			sentOrders.push(order);
		} else {
			console.log(`Unable to publish a message! Response: ${response.status} ${response.statusText}`)
		}
	}

	return new Response(`Successfully published these messages: \n\n${JSON.stringify(sentOrders, null, ' ')}`);
}
