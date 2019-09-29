package main

import (
	"encoding/json"
	"fmt"
	"html"
	"io/ioutil"
	"log"
	"net/http"
	"os"
)

//easyjson:json
type FullResponse []Response

//easyjson:json
type Response struct {
	Id   string
	Name string
	Time int64
}

func main() {
	url := "http://" + os.Getenv("HOST") + ":5500/data"
	tr := &http.Transport{
		MaxIdleConns:        4000,
		MaxIdleConnsPerHost: 4000,
	}
	client := &http.Client{Transport: tr}

	http.HandleFunc("/testReflection", func(w http.ResponseWriter, r *http.Request) {
		rsp, err := client.Get(url)
		if err != nil {
			serverError(w, err.Error())
			return
		}

		defer rsp.Body.Close()

		// deserialize
		obj := []Response{}
		err = json.NewDecoder(rsp.Body).Decode(&obj)
		if err != nil {
			serverError(w, err.Error())
			return
		}

		// serialize
		jsonStr, err := json.Marshal(&obj)
		if err != nil {
			serverError(w, err.Error())
			return
		}

		w.Header().Set("Content-Type", "application/json")
		if _, err := w.Write(jsonStr); err != nil {
			serverError(w, err.Error())
			return
		}
	})

	http.HandleFunc("/testNoReflection", func(w http.ResponseWriter, r *http.Request) {
		rsp, err := client.Get(url)
		if err != nil {
			serverError(w, err.Error())
			return
		}

		defer rsp.Body.Close()

		// deserialize
		obj := FullResponse{}
		dt, err := ioutil.ReadAll(rsp.Body)
		if err != nil {
			serverError(w, err.Error())
			return
		}

		err = obj.UnmarshalJSON(dt)
		if err != nil {
			serverError(w, err.Error())
			return
		}

		// serialize
		jsonStr, err := obj.MarshalJSON()
		if err != nil {
			serverError(w, err.Error())
			return
		}

		w.Header().Set("Content-Type", "application/json")
		if _, err := w.Write(jsonStr); err != nil {
			serverError(w, err.Error())
			return
		}
	})

	http.HandleFunc("/testNoProcess", func(w http.ResponseWriter, r *http.Request) {
		rsp, err := client.Get(url)
		if err != nil {
			serverError(w, err.Error())
			return
		}
		dt, err := ioutil.ReadAll(rsp.Body)
		if err != nil {
			serverError(w, err.Error())
			return
		}
		w.Header().Set("Content-Type", "application/json")
		if _, err := w.Write(dt); err != nil {
			serverError(w, err.Error())
			return
		}
	})

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "Hello, %q", html.EscapeString(r.URL.Path))
	})

	addr := ":5001"
	fmt.Println("listening on " + addr)
	log.Fatal(http.ListenAndServe(addr, nil))
}

func serverError(w http.ResponseWriter, msg string) {
	http.Error(w, msg, http.StatusInternalServerError)
}
