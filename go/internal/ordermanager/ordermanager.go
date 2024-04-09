package ordermanager

import (
	"context"
	"encoding/json"
	"fmt"

	dapr "github.com/dapr/go-sdk/client"

	"github.com/diagridio/catalyst-samples/go/internal/order"
	"github.com/diagridio/catalyst-samples/go/pkg/logger"
)

// orderManager -.
type orderManager struct {
	l             *logger.Logger
	pubsub, topic string
	statestore    string
	client        dapr.Client
}

// New -.
func New(l *logger.Logger, pubsub string, topic string, statestore string) *orderManager {
	client, err := dapr.NewClient()
	if err != nil {
		panic(err)
	}

	return &orderManager{
		l:          l,
		pubsub:     pubsub,
		topic:      topic,
		statestore: statestore,
		client:     client,
	}
}

func (m *orderManager) Create(ctx context.Context, o order.Order) (order.Order, error) {
	data, err := json.Marshal(o)
	if err != nil {
		return order.Order{}, fmt.Errorf("error marshaling order: %w", err)
	}

	m.l.Debug("publishing order: %+v", o)
	if err := m.client.PublishEvent(ctx, m.pubsub, m.topic, data); err != nil {
		return order.Order{}, fmt.Errorf("error publishing to topic: %w", err)
	}

	return o, nil
}

func (m *orderManager) OnNewOrder(ctx context.Context, o order.Order) error {
	m.l.Debug("got new order: %+v", o)

	// Save order to statestore
	data, err := json.Marshal(o)
	if err != nil {
		return fmt.Errorf("error marshaling order: %w", err)
	}

	if err := m.client.SaveState(ctx, m.statestore, o.ID, data, nil); err != nil {
		return fmt.Errorf("error saving order to statestore: %w", err)
	}

	return nil
}

func (m *orderManager) List(ctx context.Context) ([]order.Order, error) {
	return []order.Order{}, nil
}

func (m *orderManager) Get(ctx context.Context, id string) (order.Order, error) {
	item, err := m.client.GetState(ctx, m.statestore, id, nil)
	if err != nil {
		return order.Order{}, fmt.Errorf("error getting order from statestore: %w", err)
	}

	var o order.Order
	if err := json.Unmarshal(item.Value, &o); err != nil {
		return order.Order{}, fmt.Errorf("error unmarshaling order: %w", err)
	}

	return o, nil
}

func (m *orderManager) Delete(ctx context.Context, id string) error {
	if err := m.client.DeleteState(ctx, m.statestore, id, nil); err != nil {
		return fmt.Errorf("error deleting order from statestore: %w", err)
	}

	return nil
}
