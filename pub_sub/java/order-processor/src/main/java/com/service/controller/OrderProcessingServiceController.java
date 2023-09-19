package com.service.controller;

import io.dapr.Topic;
import io.dapr.client.DaprClient;
import io.dapr.client.DaprClientBuilder;
import io.dapr.client.domain.CloudEvent;

import io.dapr.client.domain.State;
import lombok.Getter;
import lombok.Setter;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import reactor.core.publisher.Mono;
import javax.annotation.PostConstruct;

@RestController
public class OrderProcessingServiceController {
    private static final Logger logger = LoggerFactory.getLogger(OrderProcessingServiceController.class);
    private static final String DAPR_STATE_STORE = "kvstore";
    private DaprClient client;

    @PostConstruct
    public void init() {
        client = new DaprClientBuilder().build();
    }

    @Topic(name = "orders", pubsubName = "pubsub")
    @PostMapping(path = "/orders", consumes = MediaType.ALL_VALUE)
    public Mono<ResponseEntity> getCheckout(@RequestBody(required = false) CloudEvent<Order> cloudEvent) {
        return Mono.fromSupplier(() -> {
            try {
                int orderId = cloudEvent.getData().getOrderId();
                logger.info("Subscriber received: " + orderId);

                Order order = new Order();
                order.setOrderId(orderId);
                client.saveState(DAPR_STATE_STORE, String.valueOf(orderId), order).block();

                logger.info("Order successfully persisted: " + orderId);
                return ResponseEntity.ok("SUCCESS");
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
    }
}

@Getter
@Setter
class Order {
    private int orderId;
}