package ordermanager

import (
	"context"

	"github.com/diagridio/catalyst-samples/go/internal/order"
)

type (
	// Manager -.
	Manager interface {
		Create(context.Context, order.Order) (order.Order, error)
		List(context.Context) ([]order.Order, error)
		Get(context.Context, string) (order.Order, error)
		Delete(context.Context, string) error

		OnNewOrder(context.Context, order.Order) error
	}
)
