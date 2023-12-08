function displayData(dictionary) {
    console.log(dictionary);

    for (let key in dictionary) {
        let element = document.getElementById(key)
        element.classList.remove("visually-hidden")
        
        let value = dictionary[key]
    }
}