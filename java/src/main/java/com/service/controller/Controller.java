package com.service.controller;

import io.dapr.client.DaprClient;
import io.dapr.client.DaprClientBuilder;
import io.dapr.client.domain.CloudEvent;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.time.Duration;

import io.dapr.client.domain.State;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import org.json.JSONObject;
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
    private HttpClient httpClient;

    private static final String PUBSUB_NAME = System.getenv().getOrDefault("PUBSUB_NAME", "pubsub");
    private static final String KVSTORE_NAME = System.getenv().getOrDefault("KVSTORE_NAME", "kvstore");
    private static final String DAPR_HTTP_ENDPOINT = System.getenv().getOrDefault("DAPR_HTTP_ENDPOINT", "http://localhost");
    private static final String DAPR_API_TOKEN = System.getenv().getOrDefault("DAPR_API_TOKEN", "");
    private static final String INVOKE_TARGET_APPID = System.getenv().getOrDefault("INVOKE_APPID", "target");

    @PostConstruct
    public void init() {
        client = new DaprClientBuilder().build();
        httpClient = HttpClient.newBuilder()
                .version(HttpClient.Version.HTTP_2)
                .connectTimeout(Duration.ofSeconds(10))
                .build();
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
                logger.info("Order received: " + orderId);
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
                JSONObject obj = new JSONObject();
                obj.put("orderId", order.getOrderId());

                HttpRequest request = HttpRequest.newBuilder()
                        .POST(HttpRequest.BodyPublishers.ofString(obj.toString()))
                        .uri(URI.create(DAPR_HTTP_ENDPOINT + "/invoke/neworders"))
                        .header("dapr-api-token", DAPR_API_TOKEN)
                        .header("Content-Type", "application/json")
                        .header("dapr-app-id", INVOKE_TARGET_APPID)
                        .build();

                HttpResponse<String> response = httpClient.send(request, HttpResponse.BodyHandlers.ofString());
                JSONObject jsonObject = new JSONObject(response.body());
                logger.info("Invoke Successful. Reply received: " + jsonObject.getInt("orderId"));
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
    @DeleteMapping(path = "/kv/orders/{orderId}", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> deleteKV(@PathVariable String orderId) {
        return Mono.fromSupplier(() -> {

            try {
                Void response = client.deleteState(KVSTORE_NAME, "" + orderId).block();
                logger.info("Delete KV Successful. Order deleted: " + orderId);
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                logger.error("Error occurred while deleting order: " + orderId);
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