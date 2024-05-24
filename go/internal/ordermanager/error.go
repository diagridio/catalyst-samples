package ordermanager

type errOrderNotFound struct{}

func (errOrderNotFound) Error() string {
	return "order not found"
}

func IsOrderNotFoundError(err error) bool {
	_, ok := err.(errOrderNotFound)
	return ok
}
