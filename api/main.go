package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"log"
	"math/rand"
	"strconv"
	"time"

	"github.com/valyala/fasthttp"
)

type Response struct {
	Id   string
	Name string
	Time int64
}

const jsonSize = 10

var (
	addr = flag.String("addr", ":5500", "TCP address to listen to")
)

func main() {
	flag.Parse()
	h := requestHandler
	if err := fasthttp.ListenAndServe(*addr, h); err != nil {
		log.Fatalf("Error in ListenAndServe: %s", err)
	}
}

func requestHandler(ctx *fasthttp.RequestCtx) {
	if string(ctx.Path()) == "/data" {

		res := make([]Response, jsonSize)
		for i := 0; i < jsonSize; i++ {
			rsp := Response{
				Id:   "id_" + strconv.Itoa(rand.Int()),
				Name: "name_" + strconv.Itoa(rand.Int()),
				Time: time.Now().Unix(),
			}
			res[i] = rsp
		}

		js, err := json.Marshal(res)
		if err != nil {
			ctx.Error(err.Error(), fasthttp.StatusForbidden)
			return
		}
		fmt.Fprintf(ctx, "%s\n", js)
		ctx.SetContentType("application/json")
		return
	}

	ctx.Error("Forbidden", fasthttp.StatusForbidden)
	return
}
