package com.service;

import io.dapr.client.DaprClient;
import io.dapr.client.DaprClientBuilder;
import lombok.AllArgsConstructor;
import lombok.Getter;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

import java.util.Map;
import java.util.concurrent.TimeUnit;

@SpringBootApplication
public class PublisherApplication {
	private static final Logger logger = LoggerFactory.getLogger(PublisherApplication.class);

	public static void main(String[] args) throws Exception {
		Map<String, String> envVariables = System.getenv();
		SpringApplication.run(PublisherApplication.class, args);
		String TOPIC_NAME = "orders";
		String PUBSUB_NAME = "pubsub";
		DaprClient client = new DaprClientBuilder().build();

		for (int i = 0;  ; i++) {
			int orderId = i;
			Order order = new Order(orderId);

			// Publish an event/message using Dapr PubSub
			client.publishEvent(
					PUBSUB_NAME,
					TOPIC_NAME,
					order).block();
			logger.info("Published data: " + order.getOrderId());
			TimeUnit.MILLISECONDS.sleep(5000);
		}
	}
}

@AllArgsConstructor
@Getter
class Order {
	private int orderId;
}