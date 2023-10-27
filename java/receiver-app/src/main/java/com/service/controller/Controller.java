package com.service.controller;

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

    @PostConstruct
    public void init() {
        client = new DaprClientBuilder().build();
    }

    @PostMapping(path = "/receiverequest", consumes = MediaType.ALL_VALUE)
    public ResponseEntity<Order> reply(@RequestBody Order order) {
        System.out.println("Request received : " + order.getOrderId());
        return ResponseEntity.ok(order);
    }

    @PostMapping(path = "/consume", consumes = MediaType.ALL_VALUE)
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
}

@Getter
@Setter
@ToString
class Order {
    private int orderId;
}