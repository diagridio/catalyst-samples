package com.service.controller;

import io.dapr.Topic;
import io.dapr.client.DaprClient;
import io.dapr.client.DaprClientBuilder;
import io.dapr.client.domain.CloudEvent;

import io.dapr.client.domain.HttpExtension;
import io.dapr.client.domain.State;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import reactor.core.publisher.Mono;
import javax.annotation.PostConstruct;

@RestController
public class Controller {
    private static final Logger logger = LoggerFactory.getLogger(Controller.class);
    private DaprClient client;

    private static final String PUBSUB_NAME = System.getenv().getOrDefault("PUBSUB_NAME", "pubsub");
    private static final String KVSTORE_NAME = System.getenv().getOrDefault("KVSTORE_NAME", "kvstore");

    @PostConstruct
    public void init() {
        client = new DaprClientBuilder().build();
    }

    // Publish messages 
    @PostMapping(path = "/pubsub/orders", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> publish(@RequestBody(required = true) Order order) {
        return Mono.fromSupplier(() -> {

            // Publish an event/message using Dapr PubSub
            try {
                client.publishEvent(PUBSUB_NAME, "orders", order).block();
                logger.info("Publish Successful. Order published: " + order.getOrderId());
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                logger.error("Error occurred while publishing order: " + order.getOrderId());
                throw new RuntimeException(e);
            }
        });
    }

    // Subscribe to messages
    @PostMapping(path = "/pubsub/neworders", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> subscribe(@RequestBody(required = false) CloudEvent<Order> cloudEvent) {
        return Mono.fromSupplier(() -> {
            try {
                int orderId = cloudEvent.getData().getOrderId();
                logger.info("Message received: " + orderId);
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
    }

    // Invoke another service
    @PostMapping(path = "/invoke/orders", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> request(@RequestBody(required = true) Order order) {
        return Mono.fromSupplier(() -> {
            try {
                Order response = client.invokeMethod("target", "invoke/neworders", order, HttpExtension.POST, Order.class).block();
                logger.info("Invoke Successful. Reply received: " + response.getOrderId());
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                logger.error("Error occurred while invoking App ID. " + e);
                throw new RuntimeException(e);
            }
        });
    }

    // Service to be invoked
    @PostMapping(path = "/invoke/neworders", consumes = MediaType.ALL_VALUE)
    public ResponseEntity<Order> reply(@RequestBody Order order) {
        System.out.println("Request received : " + order.getOrderId());
        return ResponseEntity.ok(order);
    }

    // Save state
    @PostMapping(path = "/kv/orders", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> saveKV(@RequestBody(required = true) Order order) {
        return Mono.fromSupplier(() -> {
            try {
                Void response = client.saveState(KVSTORE_NAME, "" + order.getOrderId(), order).block();
                logger.info("Save KV Successful. Order saved: " + order.getOrderId());
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                logger.error("Error occurred while saving order: " + order.getOrderId());
                throw new RuntimeException(e);
            }
        });
    }

    // Retrieve state
    @GetMapping(path = "/kv/orders/{orderId}", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> getKV(@PathVariable String orderId) {
        return Mono.fromSupplier(() -> {
            Order responseOrder = null;
            try {
                State<Order> response = client.getState(KVSTORE_NAME, "" + orderId, Order.class).block();
                responseOrder = response.getValue();
                logger.info("Get KV Successful. Order retrieved: " + responseOrder);
                return ResponseEntity.ok(responseOrder);
            } catch (Exception e) {
                logger.error("Error occurred while retrieving order: " + responseOrder);
                throw new RuntimeException(e);
            }
        });
    }

    // Delete state
    @DeleteMapping(path = "/kv/orders", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> deleteKV(@RequestBody(required = true) Order order) {
        return Mono.fromSupplier(() -> {

            try {
                Void response = client.deleteState(KVSTORE_NAME, "" + order.getOrderId()).block();
                logger.info("Delete KV Successful. Order deleted: " + order.getOrderId());
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                logger.error("Error occurred while deleting order: " + order.getOrderId());
                throw new RuntimeException(e);
            }
        });
    }
}

@Getter
@Setter
@ToString
class Order {
    private int orderId;
}