import random

def generate_random_temperature(min_temp=30.0, max_temp=36.0):

    return round(random.uniform(min_temp, max_temp), 2)

# Example usage
random_temperature = generate_random_temperature()
print(f"Random temperature: {random_temperature} °C")

