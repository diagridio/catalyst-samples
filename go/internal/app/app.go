// Package app configures and runs application.
package app

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"os/signal"
	"syscall"

	"github.com/dapr/go-sdk/service/common"
	daprd "github.com/dapr/go-sdk/service/grpc"
	"github.com/gin-gonic/gin"

	"github.com/diagridio/catalyst-samples/go/config"
	v1 "github.com/diagridio/catalyst-samples/go/internal/controller/http/v1"
	"github.com/diagridio/catalyst-samples/go/internal/order"
	"github.com/diagridio/catalyst-samples/go/internal/ordermanager"
	"github.com/diagridio/catalyst-samples/go/pkg/httpserver"
	"github.com/diagridio/catalyst-samples/go/pkg/logger"
)

// Run creates objects via constructors.
func Run(cfg *config.Config) {
	l := logger.New(cfg.Log.Level)

	manager := ordermanager.New(l, cfg.Pubsub.Name, cfg.Pubsub.Topic, cfg.Statestore.Name)

	// HTTP Server
	handler := gin.New()
	v1.NewRouter(handler, l, manager)
	httpServer := httpserver.New(handler, httpserver.Port(cfg.HTTP.Port))

	// Dapr
	daprSrv, err := daprd.NewService(":" + cfg.Dapr.Port)
	if err != nil {
		l.Fatal("failed to start dapr service: %v", err)
	}

	if err := daprSrv.AddTopicEventHandler(
		&common.Subscription{
			PubsubName: cfg.Pubsub.Name,
			Topic:      cfg.Pubsub.Topic,
			Route:      "/pubsub/neworder",
		},
		func(ctx context.Context, e *common.TopicEvent) (bool, error) {
			l.Debug("app - Run - event: %+v", *e)
			var o order.Order
			if err := json.Unmarshal(e.RawData, &o); err != nil {
				return false, fmt.Errorf("error unmarshaling order: %w", err)
			}

			if err := manager.OnNewOrder(ctx, o); err != nil {
				return false, fmt.Errorf("error handling new order: %w", err)
			}

			return false, nil
		}); err != nil {
		l.Fatal("error adding topic subscription: %v", err)
	}

	if err := daprSrv.AddHealthCheckHandler("health",
		func(context.Context) error {
			l.Debug("app - Run - health check")
			return nil
		}); err != nil {
		l.Fatal("error adding health check handler: %v", err)
	}

	if err := daprSrv.Start(); err != nil {
		l.Fatal("server error: %v", err)
	}

	// Waiting signal
	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt, syscall.SIGTERM)

	select {
	case s := <-interrupt:
		l.Info("app - Run - signal: " + s.String())
	case err := <-httpServer.Notify():
		l.Error(fmt.Errorf("app - Run - httpServer.Notify: %w", err))
	}

	// Shutdown
	if err := httpServer.Shutdown(); err != nil {
		l.Error(fmt.Errorf("app - Run - httpServer.Shutdown: %w", err))
	}
}
