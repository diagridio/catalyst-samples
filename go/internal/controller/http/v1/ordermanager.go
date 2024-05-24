package v1

import (
	"net/http"

	"github.com/gin-gonic/gin"

	"github.com/diagridio/catalyst-samples/go/internal/order"
	"github.com/diagridio/catalyst-samples/go/internal/ordermanager"
	"github.com/diagridio/catalyst-samples/go/pkg/logger"
)

type orderRoutes struct {
	manager ordermanager.Manager
	log     logger.Interface
}

type getOrderRequest struct {
	ID string `json:"id"`
}

func newOrderRoutes(handler *gin.RouterGroup, m ordermanager.Manager, l logger.Interface) {
	r := &orderRoutes{
		manager: m,
		log:     l,
	}

	h := handler.Group("/orders")
	{
		h.GET("", r.list)
		h.GET("/:id", r.get)
		h.POST("", r.create)
		h.DELETE("/:id", r.del)
	}
}

// @Summary     List Orders
// @Description List all orders
// @ID          id
// @Tags  	    order
// @Accept      json
// @Produce     json
// @Success     200 {object} listOrderResponse
// @Failure     500 {object} response
// @Router      /orders [get]
func (r *orderRoutes) list(c *gin.Context) {
	orders, err := r.manager.List(c.Request.Context())
	if err != nil {
		r.log.Error(err, "http - v1 - get order")
		errorResponse(c, http.StatusInternalServerError, "failed to get order")

		return
	}

	c.JSON(http.StatusOK, orders)
}

// @Summary     Get Order
// @Description Fetch an order by id
// @ID          id
// @Tags  	    order
// @Accept      json
// @Produce     json
// @Success     200 {object} getOrderResponse
// @Failure     500 {object} response
// @Router      /orders/:id [get]
func (r *orderRoutes) get(c *gin.Context) {
	id := c.Param("id")

	order, err := r.manager.Get(c.Request.Context(), id)
	if err != nil {
		if ordermanager.IsOrderNotFoundError(err) {
			errorResponse(c, http.StatusNotFound, "order not found")

			return
		}

		r.log.Error(err, "http - v1 - get order")
		errorResponse(c, http.StatusInternalServerError, "failed to get order")

		return
	}

	c.JSON(http.StatusOK, order)
}

// @Summary     Create Order
// @Description Create an order
// @ID          id
// @Tags  	    order
// @Accept      json
// @Produce     json
// @Success     200 {object} createOrderResponse
// @Failure     500 {object} response
// @Router      /orders [post]
func (r *orderRoutes) create(c *gin.Context) {
	var o order.Order

	// Call BindJSON to bind the received JSON to
	// newAlbum.
	if err := c.BindJSON(&o); err != nil {
		return
	}

	o, err := r.manager.Create(c.Request.Context(), o)
	if err != nil {
		r.log.Error(err, "http - v1 - create order")
		errorResponse(c, http.StatusInternalServerError, "failed to create order")

		return
	}

	c.IndentedJSON(http.StatusCreated, o)
}

// @Summary     Delete Order
// @Description Delete an order
// @ID          id
// @Tags  	    order
// @Accept      json
// @Produce     json
// @Success     200 {object} deleteOrderResponse
// @Failure     500 {object} response
// @Router      /orders [post]
func (r *orderRoutes) del(c *gin.Context) {
	id := c.Param("id")

	if err := r.manager.Delete(c.Request.Context(), id); err != nil {
		if ordermanager.IsOrderNotFoundError(err) {
			errorResponse(c, http.StatusNotFound, "order not found")

			return
		}

		r.log.Error(err, "http - v1 - delete order")
		errorResponse(c, http.StatusInternalServerError, "failed to delete order")

		return
	}
}
