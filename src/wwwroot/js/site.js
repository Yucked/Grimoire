function displayData(dictionary) {
    console.log(dictionary);

    for (let key in dictionary) {
        console.log(key)
        
        let element = document.getElementById(key)
        element.classList.remove("visually-hidden")
        
        let value = dictionary[key]
        let div = document.createElement("div")
        div.classList.add("container")
        div.classList.add("overflow-x-auto")
        
    }
}