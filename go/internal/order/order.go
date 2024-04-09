package order

type Order struct {
	ID        string `json:"orderId"`
	ProductId string `json:"productId"`
	Quantity  int    `json:"quantity"`
}
