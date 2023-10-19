export default {
	async fetch(request, env, ctx) {
		const url = new URL(request.url);
		switch (url.pathname) {
			case '/':
				return rootHandler(request, env);
			case '/orders':
				return orderHandler(request, env);
			default:
				return new Response("Not found", { status: 404 })
		}
	},
};

async function rootHandler(request, env) {
	if (request.method === "GET") {
		const values = await getKVPairs(env);
		return new Response(`Orders received: \n\n${JSON.stringify(values, null, ' ')}`);
	} else if (request.method === "POST") {
		return new Response("Received POST message not on /orders endpoint. Request must be a POST sent to /orders endpoint!");
	}
}

async function getKVPairs(env) {

	// Get KV data using Dapr State Store API
	// If the keys are not found the keys are echoed back in the response without value.
	const stateUrl = `${env.DAPR_HOST}:${env.DAPR_HTTP_PORT}/v1.0/state/${env.STORAGE_ACCOUNT_NAME}/bulk`;
	console.log(stateUrl);
	const stateBody = { keys: ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"] };
	const init = {
		body: JSON.stringify(stateBody),
		method: 'POST',
		headers: {
			'content-type': 'application/json',
			'dapr-api-token': `${env.ORDERPROCESSOR_API_TOKEN}`,
		},
	};
	const stateResponse = await fetch(stateUrl, init);
	let kvpairs = await stateResponse.json();
	return kvpairs;
}

async function orderHandler(request, env) {
	let contentType = request.headers.get('content-type');
	console.log(`Content type: ${contentType}`);

	if (request.method === "POST" && (contentType === 'application/cloudevents+json' || contentType === 'application/json')) {
		console.log('Received message on the /orders endpoint!');
		const json = await request.json();
		if (json.data) {
			const order = json.data;
			console.log('Request body order: ' + JSON.stringify(order));
			const orderId = `${order.orderId}`;
			console.log('order id: ' + orderId);

			// Store in KV using Dapr State Store API
			const stateBody = [{ key: orderId, value: order }];
			const stateUrl = `${env.DAPR_HOST}:${env.DAPR_HTTP_PORT}/v1.0/state/${env.STORAGE_ACCOUNT_NAME}`;
			console.log('Attempting to save state: ' + stateUrl);

			// save order into state store
			const init = {
				body: JSON.stringify(stateBody),
				method: 'POST',
				headers: {
					'content-type': 'application/json',
					'dapr-api-token': `${env.ORDERPROCESSOR_API_TOKEN}`,
				},
			};
			const stateResponse = await fetch(stateUrl, init);
			console.log('state response: ' + stateResponse.status + ' ' + stateResponse.statusText);
			const message = stateResponse.ok ? `Order ${orderId} saved to state store!` : `Failed to save order ${orderId} to state store!`;
			console.log(message);
			return new Response(message);
		} else {
			return new Response("Received message on /orders endpoint but no data found in request body!");
		}
	}
}